import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Counter } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5235').replace(/\/+$/, '');
const SEED_FILE = __ENV.SEED_FILE || '../output/aqarios_seed.json';
const PROFILE = (__ENV.PROFILE || 'load').toLowerCase();

const seed = JSON.parse(open(SEED_FILE));

if (!seed.owners || seed.owners.length === 0) {
  throw new Error(`Seed file has no owners: ${SEED_FILE}`);
}

const owners = seed.owners.filter((o) => o.access_token);
const tenants = seed.owners.flatMap((o) => o.tenant_accounts || []).filter((t) => t.access_token);
const listingIds = seed.owners.flatMap((o) => o.marketplace_listings || []);

if (owners.length === 0) {
  throw new Error('No owner access tokens found in seed JSON.');
}

if (tenants.length === 0) {
  throw new Error('No activated tenant access tokens found in seed JSON.');
}

const rateLimited = new Rate('rate_limited');
const unexpectedStatus = new Rate('unexpected_status');
const ownerRequests = new Counter('owner_requests');
const tenantRequests = new Counter('tenant_requests');
const anonymousRequests = new Counter('anonymous_requests');

const profiles = {
  smoke: {
    stages: [
      { duration: '20s', target: 1 },
    ],
    gracefulRampDown: '5s',
  },

  load: {
    stages: [
      { duration: '30s', target: 10 },
      { duration: '1m',  target: 10 },
      { duration: '30s', target: 25 },
      { duration: '2m',  target: 25 },
      { duration: '30s', target: 50 },
      { duration: '2m',  target: 50 },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },

  stress: {
    stages: [
      { duration: '30s', target: 25 },
      { duration: '1m',  target: 50 },
      { duration: '1m',  target: 100 },
      { duration: '1m',  target: 150 },
      { duration: '1m',  target: 200 },
      { duration: '1m',  target: 250 },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },

  spike: {
    stages: [
      { duration: '30s', target: 10 },
      { duration: '5s',  target: 150 },
      { duration: '30s', target: 150 },
      { duration: '5s',  target: 10 },
      { duration: '30s', target: 10 },
      { duration: '10s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },

  breakpoint: {
    stages: [
      { duration: '20s', target: 10 },
      { duration: '40s', target: 25 },
      { duration: '40s', target: 50 },
      { duration: '40s', target: 100 },
      { duration: '40s', target: 150 },
      { duration: '40s', target: 200 },
      { duration: '40s', target: 300 },
      { duration: '40s', target: 400 },
      { duration: '20s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },
};

if (!profiles[PROFILE]) {
  throw new Error(
    `Unknown PROFILE '${PROFILE}'. Use smoke, load, stress, spike, or breakpoint.`
  );
}

export const options = {
  ...profiles[PROFILE],

  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    rate_limited: ['rate==0'],
    unexpected_status: ['rate<0.01'],
  },

  summaryTrendStats: ['avg', 'med', 'min', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

function pick(array) {
  return array[Math.floor(Math.random() * array.length)];
}

function request(path, token, persona, endpointName) {
  const headers = token
    ? { Authorization: `Bearer ${token}` }
    : {};

  const res = http.get(`${BASE_URL}${path}`, {
    headers,
    tags: {
      persona,
      endpoint: endpointName,
      profile: PROFILE,
    },
  });

  const is429 = res.status === 429;
  const ok = res.status === 200;

  rateLimited.add(is429);
  unexpectedStatus.add(!ok && !is429);

  check(res, {
    [`${persona}/${endpointName} status=200`]: (r) => r.status === 200,
  });

  return res;
}

function ownerAction() {
  const owner = pick(owners);
  const token = owner.access_token;

  const endpoints = [
    ['/api/v1/auth/me', 'auth_me'],
    ['/api/v1/dashboard/summary', 'dashboard_summary'],
    ['/api/v1/buildings', 'buildings'],
    ['/api/v1/apartments', 'apartments'],
    ['/api/v1/leasing/tenants?searchTerm=', 'tenants_search'],
    ['/api/v1/leasing/contracts/search?searchTerm=&pageSize=20', 'contracts_search'],
    ['/api/v1/rent-payments?pageSize=20', 'rent_payments'],
    ['/api/v1/maintenance-requests?pageSize=20', 'maintenance_requests'],
    ['/api/v1/notifications/me?pageSize=20', 'notifications_me'],
  ];

  const [path, name] = pick(endpoints);
  ownerRequests.add(1);
  request(path, token, 'owner', name);
}

function tenantAction() {
  const tenant = pick(tenants);
  const token = tenant.access_token;

  const endpoints = [
    ['/api/v1/tenant-portal/me', 'tenant_me'],
    ['/api/v1/tenant-portal/lease', 'tenant_lease'],
    ['/api/v1/tenant-portal/payments', 'tenant_payments'],
    ['/api/v1/notifications/me?pageSize=20', 'tenant_notifications'],
  ];

  const [path, name] = pick(endpoints);
  tenantRequests.add(1);
  request(path, token, 'tenant', name);
}

function anonymousAction() {
  if (listingIds.length > 0 && Math.random() < 0.35) {
    const listingId = pick(listingIds);
    anonymousRequests.add(1);
    request(
      `/api/v1/marketplace/listings/${listingId}`,
      null,
      'anonymous',
      'marketplace_detail'
    );
    return;
  }

  anonymousRequests.add(1);
  request(
    '/api/v1/marketplace/listings',
    null,
    'anonymous',
    'marketplace_list'
  );
}

function think() {
  let min = Number(__ENV.THINK_MIN);
  let max = Number(__ENV.THINK_MAX);

  if (!Number.isFinite(min) || !Number.isFinite(max)) {
    const defaults = {
      smoke: [1.0, 1.0],
      load: [0.7, 1.5],
      stress: [0.25, 0.7],
      spike: [0.15, 0.4],
      breakpoint: [0.05, 0.2],
    };

    [min, max] = defaults[PROFILE];
  }

  if (max < min) {
    [min, max] = [max, min];
  }

  sleep(min + Math.random() * (max - min));
}

export default function () {
  // Approximate persona mix per request:
  // 70% owner, 25% tenant, 5% anonymous.
  const roll = Math.random();

  if (roll < 0.70) {
    ownerAction();
  } else if (roll < 0.95) {
    tenantAction();
  } else {
    anonymousAction();
  }

  think();
}
