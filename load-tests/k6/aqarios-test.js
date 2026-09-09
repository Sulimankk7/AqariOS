import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5235').replace(/\/+$/, '');
const SEED_FILE = __ENV.SEED_FILE || '../output/aqarios_seed.json';
const PROFILE = (__ENV.PROFILE || 'smoke').toLowerCase();
const RUN_TAG = __ENV.RUN_TAG || new Date().toISOString().replace(/[:.]/g, '-');
const seed = JSON.parse(open(SEED_FILE));
const owners = (seed.owners || []).filter((item) => item.access_token);
const tenants = (seed.owners || []).flatMap((item) => item.tenant_accounts || []).filter((item) => item.access_token);
const listingIds = (seed.owners || []).flatMap((item) => item.marketplace_listings || []);

if (!owners.length) throw new Error(`Seed file has no authenticated owners: ${SEED_FILE}`);
if (!tenants.length) throw new Error(`Seed file has no authenticated tenants: ${SEED_FILE}`);

function numberEnv(name, fallback) {
  const value = Number(__ENV[name]);
  return Number.isFinite(value) && value > 0 ? value : fallback;
}

const profiles = {
  smoke: { executor: 'shared-iterations', vus: 1, iterations: 1, maxDuration: '1m' },
  load: { executor: 'ramping-vus', startVUs: 0, stages: [{ duration: '30s', target: 10 }, { duration: '2m', target: 10 }, { duration: '30s', target: 25 }, { duration: '2m', target: 25 }, { duration: '30s', target: 0 }], gracefulRampDown: '10s' },
  mixed: { executor: 'shared-iterations', vus: numberEnv('MIXED_VUS', 5), iterations: numberEnv('MIXED_ITERATIONS', 20), maxDuration: __ENV.MIXED_MAX_DURATION || '5m' },
  stress: { executor: 'ramping-vus', startVUs: 0, stages: [{ duration: '30s', target: 25 }, { duration: '1m', target: 50 }, { duration: '1m', target: 100 }, { duration: '1m', target: 150 }, { duration: '1m', target: 200 }, { duration: '30s', target: 0 }], gracefulRampDown: '15s' },
  spike: { executor: 'ramping-vus', startVUs: 0, stages: [{ duration: '20s', target: 10 }, { duration: '5s', target: 150 }, { duration: '30s', target: 150 }, { duration: '10s', target: 10 }, { duration: '20s', target: 0 }], gracefulRampDown: '15s' },
  soak: { executor: 'constant-vus', vus: numberEnv('SOAK_VUS', 20), duration: __ENV.SOAK_DURATION || '30m', gracefulStop: '30s' },
};

if (!profiles[PROFILE]) throw new Error(`Unknown PROFILE '${PROFILE}'. Use smoke, load, mixed, stress, spike, or soak.`);

export const options = {
  scenarios: { aqarios: profiles[PROFILE] },
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    unexpected_status: ['rate<0.01'],
    server_errors: ['rate==0'],
  },
  summaryTrendStats: ['avg', 'med', 'min', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

const unexpectedStatus = new Rate('unexpected_status');
const serverErrors = new Rate('server_errors');
const rateLimited = new Counter('status_429');
const status401 = new Counter('status_401');
const status403 = new Counter('status_403');
const status404 = new Counter('status_404');
const status409 = new Counter('status_409');
const status422 = new Counter('status_422');
const endpointLatency = new Trend('endpoint_latency', true);
const writesCreated = new Counter('writes_created');

function pick(items) { return items[Math.floor(Math.random() * items.length)]; }
function auth(token) { return { Authorization: `Bearer ${token}`, Accept: 'application/json' }; }
function safeJson(res) { try { return res.json(); } catch (_) { return null; } }

function record(res, endpoint, expected) {
  const accepted = expected.includes(res.status);
  unexpectedStatus.add(!accepted && res.status !== 429, { endpoint, status: String(res.status) });
  serverErrors.add(res.status >= 500, { endpoint, status: String(res.status) });
  endpointLatency.add(res.timings.duration, { endpoint, status: String(res.status) });
  if (res.status === 429) rateLimited.add(1, { endpoint });
  if (res.status === 401) status401.add(1, { endpoint });
  if (res.status === 403) status403.add(1, { endpoint });
  if (res.status === 404) status404.add(1, { endpoint });
  if (res.status === 409) status409.add(1, { endpoint });
  if (res.status === 422) status422.add(1, { endpoint });
  check(res, {
    [`${endpoint}: expected status`]: () => accepted,
    [`${endpoint}: valid JSON body`]: (r) => !r.body || !(r.headers['Content-Type'] || '').includes('json') || safeJson(r) !== null,
  });
  return res;
}

function get(path, token, endpoint, expected = [200]) {
  return record(http.get(`${BASE_URL}${path}`, { headers: token ? auth(token) : { Accept: 'application/json' }, tags: { endpoint, profile: PROFILE, operation: 'read' } }), endpoint, expected);
}

function post(path, token, endpoint, body, expected) {
  return record(http.post(`${BASE_URL}${path}`, JSON.stringify(body), { headers: { ...auth(token), 'Content-Type': 'application/json' }, tags: { endpoint, profile: PROFILE, operation: 'write' } }), endpoint, expected);
}

function ownerRead(owner) {
  const building = pick(owner.buildings || []);
  const apartments = (owner.buildings || []).flatMap((item) => item.apartments || []);
  const apartment = apartments.length ? pick(apartments) : null;
  const tenant = (owner.tenants || []).length ? pick(owner.tenants) : null;
  const lease = (owner.leases || []).length ? pick(owner.leases) : null;
  const maintenanceId = (owner.maintenance_requests || []).length ? pick(owner.maintenance_requests) : null;
  const expenseId = (owner.expenses || []).length ? pick(owner.expenses) : null;
  const calls = [
    () => get('/api/v1/auth/me', owner.access_token, 'auth_me'),
    () => get('/api/v1/dashboard/summary', owner.access_token, 'dashboard_summary'),
    () => get('/api/v1/buildings', owner.access_token, 'buildings_list'),
    () => get('/api/v1/apartments', owner.access_token, 'apartments_list'),
    () => get('/api/v1/leasing/tenants?searchTerm=&pageSize=20', owner.access_token, 'tenants_search'),
    () => get('/api/v1/leasing/contracts/search?searchTerm=&pageSize=20', owner.access_token, 'leases_search'),
    () => get('/api/v1/rent-payments?pageSize=20', owner.access_token, 'rent_payments_list'),
    () => get('/api/v1/maintenance-requests?pageSize=20', owner.access_token, 'maintenance_list'),
    () => get('/api/v1/expenses?pageSize=20', owner.access_token, 'expenses_list'),
    () => get('/api/v1/notifications/me?pageSize=20', owner.access_token, 'notifications_list'),
    () => get('/api/v1/marketplace/listings/company?pageSize=20', owner.access_token, 'company_listings'),
  ];
  if (building) calls.push(() => get(`/api/v1/buildings/${building.id}`, owner.access_token, 'building_detail'));
  if (apartment) calls.push(() => get(`/api/v1/apartments/${apartment.id}`, owner.access_token, 'apartment_detail'));
  if (tenant) calls.push(() => get(`/api/v1/leasing/tenants/${tenant.id}`, owner.access_token, 'tenant_detail'));
  if (lease) calls.push(() => get(`/api/v1/leasing/contracts/${lease.id}`, owner.access_token, 'lease_detail'));
  if (maintenanceId) calls.push(() => get(`/api/v1/maintenance-requests/${maintenanceId}`, owner.access_token, 'maintenance_detail'));
  if (expenseId) calls.push(() => get(`/api/v1/expenses/${expenseId}`, owner.access_token, 'expense_detail'));
  pick(calls)();
}

function tenantRead(tenant) {
  pick([
    () => get('/api/v1/tenant-portal/me', tenant.access_token, 'tenant_portal_me'),
    () => get('/api/v1/tenant-portal/lease', tenant.access_token, 'tenant_portal_lease'),
    () => get('/api/v1/tenant-portal/payments', tenant.access_token, 'tenant_portal_payments'),
    () => get('/api/v1/notifications/me?pageSize=20', tenant.access_token, 'tenant_notifications'),
    () => get('/api/v1/utility-bills/my/dashboard-summary', tenant.access_token, 'tenant_utility_summary'),
  ])();
}

function anonymousRead() {
  if (listingIds.length && Math.random() < 0.4) get(`/api/v1/marketplace/listings/${pick(listingIds)}`, null, 'marketplace_detail');
  else get('/api/v1/marketplace/listings?pageSize=20', null, 'marketplace_list');
}

function smokeJourney() {
  const owner = owners[0];
  const tenant = tenants[0];
  get('/api/v1/marketplace/listings?pageSize=1', null, 'smoke_marketplace');
  get('/api/v1/auth/me', owner.access_token, 'smoke_owner_auth');
  get('/api/v1/dashboard/summary', owner.access_token, 'smoke_dashboard');
  get('/api/v1/buildings', owner.access_token, 'smoke_buildings');
  get('/api/v1/leasing/contracts/search?searchTerm=&pageSize=5', owner.access_token, 'smoke_leases');
  get('/api/v1/rent-payments?pageSize=5', owner.access_token, 'smoke_payments');
  get('/api/v1/tenant-portal/me', tenant.access_token, 'smoke_tenant_profile');
  get('/api/v1/tenant-portal/lease', tenant.access_token, 'smoke_tenant_lease');
}

function mixedJourney() {
  const owner = pick(owners);
  ownerRead(owner);
  const apartments = (owner.buildings || []).flatMap((item) => item.apartments || []);
  if (!apartments.length || !(owner.tenants || []).length) return;
  const apartment = pick(apartments);
  const tenant = pick(owner.tenants);
  const res = post('/api/v1/maintenance-requests', owner.access_token, 'maintenance_create', {
    buildingId: apartment.building_id,
    apartmentId: apartment.id,
    tenantId: tenant.id,
    title: `k6 ${RUN_TAG} vu-${__VU} iter-${__ITER}`,
    description: 'Bounded synthetic request created by the AqariOS mixed workload.',
    category: (__VU + __ITER) % 10,
    priority: (__VU + __ITER) % 4,
    requestDate: new Date(Date.now() - 86400000).toISOString().slice(0, 10),
  }, [201]);
  if (res.status === 201) writesCreated.add(1);
}

function think() {
  const min = Number(__ENV.THINK_MIN || (PROFILE === 'load' || PROFILE === 'soak' ? 0.7 : 0.15));
  const max = Number(__ENV.THINK_MAX || (PROFILE === 'load' || PROFILE === 'soak' ? 1.5 : 0.5));
  sleep(min + Math.random() * Math.max(0, max - min));
}

export default function () {
  if (PROFILE === 'smoke') return smokeJourney();
  if (PROFILE === 'mixed') return mixedJourney();
  const roll = Math.random();
  if (roll < 0.70) ownerRead(pick(owners));
  else if (roll < 0.95) tenantRead(pick(tenants));
  else anonymousRead();
  think();
}

export function handleSummary(data) {
  const output = __ENV.SUMMARY_FILE;
  return output ? { stdout: textSummary(data), [output]: JSON.stringify(data, null, 2) } : { stdout: textSummary(data) };
}

function textSummary(data) {
  const m = data.metrics;
  const value = (name, key) => m[name] && m[name].values && m[name].values[key] !== undefined ? m[name].values[key] : 'n/a';
  return [
    `\nAqariOS ${PROFILE} summary`,
    `requests=${value('http_reqs', 'count')} rps=${value('http_reqs', 'rate')}`,
    `latency_ms avg=${value('http_req_duration', 'avg')} p50=${value('http_req_duration', 'med')} p95=${value('http_req_duration', 'p(95)')} p99=${value('http_req_duration', 'p(99)')}`,
    `failed_rate=${value('http_req_failed', 'rate')} unexpected_rate=${value('unexpected_status', 'rate')} 429=${value('status_429', 'count')}`,
    `vus_max=${value('vus_max', 'max')} checks_rate=${value('checks', 'rate')}\n`,
  ].join('\n');
}
