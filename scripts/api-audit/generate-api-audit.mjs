import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '../..');
const slash = value => value.replaceAll('\\', '/');
const rel = value => slash(path.relative(root, value));

function filesUnder(dir, extensions) {
  const output = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (['bin', 'obj', 'node_modules', 'build', '.dart_tool'].includes(entry.name)) continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) output.push(...filesUnder(full, extensions));
    else if (extensions.some(ext => entry.name.endsWith(ext))) output.push(full);
  }
  return output;
}

const csFiles = [
  ...filesUnder(path.join(root, 'src', 'PropertyOS.Api'), ['.cs']),
  ...filesUnder(path.join(root, 'src', 'PropertyOS.Application'), ['.cs']),
  ...filesUnder(path.join(root, 'src', 'PropertyOS.Domain'), ['.cs']),
];
const clientFiles = [
  ...filesUnder(path.join(root, 'frontend', 'src'), ['.ts', '.tsx']),
  ...filesUnder(path.join(root, 'mobile', 'lib'), ['.dart']),
];

function splitTopLevel(value) {
  const parts = [];
  let start = 0, round = 0, square = 0, curly = 0, angle = 0;
  let quote = null;
  for (let i = 0; i < value.length; i++) {
    const c = value[i];
    if (quote) {
      if (c === quote && value[i - 1] !== '\\') quote = null;
      continue;
    }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === '(') round++; else if (c === ')') round--;
    else if (c === '[') square++; else if (c === ']') square--;
    else if (c === '{') curly++; else if (c === '}') curly--;
    else if (c === '<') angle++; else if (c === '>') angle--;
    else if (c === ',' && round === 0 && square === 0 && curly === 0 && angle === 0) {
      parts.push(value.slice(start, i).trim()); start = i + 1;
    }
  }
  const tail = value.slice(start).trim();
  if (tail) parts.push(tail);
  return parts;
}

function findBalanced(text, openAt, open = '(', close = ')') {
  let depth = 0, quote = null;
  for (let i = openAt; i < text.length; i++) {
    const c = text[i];
    if (quote) {
      if (c === quote && text[i - 1] !== '\\') quote = null;
      continue;
    }
    if (c === '"' || c === "'") { quote = c; continue; }
    if (c === open) depth++;
    if (c === close && --depth === 0) return i;
  }
  return -1;
}

function cleanType(type) {
  return type.replace(/\b(required|readonly|ref|out|in|params)\b/g, '').replace(/\s+/g, ' ').trim();
}

function parseParam(raw) {
  const binding = raw.match(/\[From(Body|Query|Route|Header|Form)(?:\([^\]]*\))?\]/)?.[1]?.toLowerCase();
  const noAttrs = raw.replace(/\[[^\]]+\]\s*/g, '').trim();
  const noDefault = noAttrs.replace(/\s*=\s*[\s\S]*$/, '').trim();
  const match = noDefault.match(/^([\w.<>,?\[\]\s:]+)\s+(\w+)$/);
  if (!match) return { raw: raw.replace(/\s+/g, ' ').trim(), binding: binding ?? 'inferred' };
  return { name: match[2], type: cleanType(match[1]), binding: binding ?? 'inferred', optional: /\?|=/.test(raw) };
}

function extractSchemas() {
  const schemas = new Map();
  for (const file of csFiles) {
    const text = fs.readFileSync(file, 'utf8');
    const lines = text.split(/\r?\n/);
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      let match = line.match(/\b(?:public\s+)?(?:sealed\s+)?(?:partial\s+)?record(?:\s+class|\s+struct)?\s+(\w+)\s*\(/);
      if (match) {
        const start = text.indexOf('(', text.indexOf(line));
        const end = findBalanced(text, start);
        if (end > start) {
          const fields = splitTopLevel(text.slice(start + 1, end)).map(parseParam)
            .filter(field => field.name && field.type && !['CancellationToken'].includes(field.type));
          if (!schemas.has(match[1])) schemas.set(match[1], { name: match[1], kind: 'record', fields, file: rel(file), line: i + 1 });
        }
        continue;
      }
      match = line.match(/\b(?:public\s+)?(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)/);
      if (match) {
        const className = match[1];
        const startOffset = text.indexOf(line);
        const brace = text.indexOf('{', startOffset);
        if (brace < 0) continue;
        const end = findBalanced(text, brace, '{', '}');
        if (end < 0) continue;
        const body = text.slice(brace + 1, end);
        const fields = [];
        const propertyRx = /public\s+(?:required\s+)?([A-Za-z_][\w.<>,?\[\]]*)\s+(\w+)\s*\{\s*get\s*;\s*(?:init|set)\s*;/g;
        let property;
        while ((property = propertyRx.exec(body))) fields.push({ name: property[2], type: cleanType(property[1]), binding: 'json' });
        if (fields.length && !schemas.has(className)) schemas.set(className, { name: className, kind: 'class', fields, file: rel(file), line: i + 1 });
      }
      match = line.match(/\b(?:public\s+)?enum\s+(\w+)/);
      if (match && !schemas.has(match[1])) {
        const brace = text.indexOf('{', text.indexOf(line));
        const end = brace >= 0 ? findBalanced(text, brace, '{', '}') : -1;
        const values = end > brace ? text.slice(brace + 1, end).replace(/\/\/.*$/gm, '').split(',').map(x => x.trim().split(/\s*=\s*/)[0]).filter(Boolean) : [];
        schemas.set(match[1], { name: match[1], kind: 'enum', values, file: rel(file), line: i + 1 });
      }
    }
    for (const declaration of text.matchAll(/\bpublic\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+)/g)) {
      if (schemas.has(declaration[1])) continue;
      const tail = text.slice(declaration.index);
      const fields = [];
      const propertyRx = /public\s+(?:required\s+)?([A-Za-z_][\w.<>,?\[\]]*)\s+(\w+)\s*\{\s*get\s*;\s*(?:init|set)\s*;/g;
      let property;
      while ((property = propertyRx.exec(tail))) fields.push({ name: property[2], type: cleanType(property[1]), binding: 'json' });
      if (fields.length) schemas.set(declaration[1], { name: declaration[1], kind: 'class', fields, file: rel(file), line: text.slice(0, declaration.index).split(/\r?\n/).length });
    }
  }
  return schemas;
}

function combineRoute(base, action, controllerName) {
  const route = action?.startsWith('/') ? action.slice(1) : [base, action].filter(Boolean).join('/');
  return ('/' + route.replace('[controller]', controllerName.replace(/Controller$/, '')))
    .replaceAll('api/v{version:apiVersion}', 'api/v1').replace(/\/+/g, '/').replace(/\/$/, '');
}

function extractEndpoints() {
  const endpoints = [];
  const verbs = { HttpGet: 'GET', HttpPost: 'POST', HttpPut: 'PUT', HttpPatch: 'PATCH', HttpDelete: 'DELETE' };
  for (const file of csFiles.filter(file => file.endsWith('Controller.cs'))) {
    const text = fs.readFileSync(file, 'utf8');
    const classMatch = text.match(/((?:\s*\[[^\]]+\]\s*)*)public\s+(?:sealed\s+)?class\s+(\w+Controller)/);
    if (!classMatch) continue;
    const classAttrs = classMatch[1];
    const controller = classMatch[2];
    const baseRoute = classAttrs.match(/\[Route\("([^"]+)"\)\]/)?.[1] ?? '';
    const classAuth = classAttrs.match(/\[Authorize(?:\(Policy\s*=\s*([^\)]+)\))?\]/)?.[1]?.trim() ?? (classAttrs.includes('[Authorize]') ? 'authenticated' : null);
    const httpRx = /\[(HttpGet|HttpPost|HttpPut|HttpPatch|HttpDelete)(?:\("([^"]*)"\))?\]/g;
    let http;
    while ((http = httpRx.exec(text))) {
      const forward = text.slice(httpRx.lastIndex, httpRx.lastIndex + 12000);
      let method = forward.match(/([\s\S]*?)public\s+async\s+Task<([\s\S]*?)>\s+(\w+)\s*\(/);
      if (!method) {
        const syncMethod = forward.match(/([\s\S]*?)public\s+(ActionResult(?:<[^>]+>)?|IActionResult)\s+(\w+)\s*\(/);
        if (syncMethod) method = syncMethod;
      }
      if (!method || method[1].includes('[Http')) continue;
      const attrsBefore = text.slice(http.index, httpRx.lastIndex) + method[1];
      const paramsStart = httpRx.lastIndex + method.index + method[0].length - 1;
      const paramsEnd = findBalanced(text, paramsStart);
      if (paramsEnd < 0) continue;
      const params = splitTopLevel(text.slice(paramsStart + 1, paramsEnd)).map(parseParam)
        .filter(p => p.type !== 'CancellationToken');
      for (const p of params) {
        if (p.binding === 'inferred') p.binding = ((http[2] ?? '') + baseRoute).includes(`{${p.name}`) ? 'route' : (verbs[http[1]] === 'GET' ? 'query' : 'route-or-query');
      }
      const responses = [];
      const responseRx = /\[ProducesResponseType\((?:typeof\(([^)]+)\),\s*)?StatusCodes\.Status(\d+)(\w+)\)\]/g;
      let response;
      while ((response = responseRx.exec(attrsBefore))) responses.push({ status: Number(response[2]), type: response[1] ?? (response[2] === '204' ? 'empty' : 'unspecified') });
      const authPolicy = attrsBefore.match(/\[Authorize\(Policy\s*=\s*([^\)]+)\)\]/)?.[1]?.trim();
      const auth = attrsBefore.includes('[AllowAnonymous]') ? 'anonymous' : authPolicy ?? (attrsBefore.includes('[Authorize]') ? 'authenticated' : classAuth ?? 'anonymous');
      const line = text.slice(0, http.index).split(/\r?\n/).length;
      const returnType = method[2].replace(/^ActionResult</, '').replace(/>$/, '').trim();
      if (!responses.length && returnType && !['IActionResult', 'ActionResult'].includes(returnType)) responses.push({ status: 200, type: returnType });
      endpoints.push({
        controller, action: method[3], method: verbs[http[1]], route: combineRoute(baseRoute, http[2] ?? '', controller),
        auth, params, bodyType: params.find(p => p.binding === 'body')?.type ?? null,
        responses, returnType, file: rel(file), line,
      });
    }
  }
  return endpoints.sort((a, b) => a.route.localeCompare(b.route) || a.method.localeCompare(b.method));
}

function extractClients() {
  const calls = [];
  const patterns = [
    { client: 'web', rx: /http\.(get|post|put|patch|delete)(?:<([^>]+)>)?\s*\(\s*([`'"])([^`'"]+)\3/g },
    { client: 'mobile', rx: /(?:_client|client)\.(getJson|getList|postJson|postVoid|putJson|putVoid|patchJson|patchVoid|deleteJson|deleteVoid|delete)\s*\(\s*([`'"])([^`'"]+)\2/g },
  ];
  for (const file of clientFiles) {
    const text = fs.readFileSync(file, 'utf8');
    const constants = new Map();
    const constRx = /(?:const|final)\s+(\w+)\s*=\s*['"]([^'"]+)['"]/g;
    let constant;
    while ((constant = constRx.exec(text))) constants.set(constant[1], constant[2]);
    const resolveRoute = raw => {
      let value = raw;
      for (const [name, replacement] of constants) {
        value = value.replaceAll(`\${${name}}`, replacement).replace(new RegExp(`\\$${name}\\b`, 'g'), replacement);
      }
      return value;
    };
    for (const pattern of patterns) {
      if ((pattern.client === 'web') !== file.includes(`${path.sep}frontend${path.sep}`)) continue;
      pattern.rx.lastIndex = 0;
      let match;
      while ((match = pattern.rx.exec(text))) {
        const op = match[1];
        const route = resolveRoute(pattern.client === 'web' ? match[4] : match[3]);
        const responseType = pattern.client === 'web' ? match[2] ?? 'inferred' : 'Map/List/void (parsed in repository)';
        const method = op.startsWith('get') ? 'GET' : op.startsWith('post') ? 'POST' : op.startsWith('put') ? 'PUT' : op.startsWith('patch') ? 'PATCH' : 'DELETE';
        calls.push({ client: pattern.client, method, route, responseType, file: rel(file), line: text.slice(0, match.index).split(/\r?\n/).length });
      }
    }
  }
  return calls.sort((a, b) => a.client.localeCompare(b.client) || a.route.localeCompare(b.route));
}

const schemas = extractSchemas();
const endpoints = extractEndpoints();
const clients = extractClients();

function routeShape(route) {
  return route.replace(/\?.*$/, '')
    .replace(/\$\{(?:query|qs)\b[\s\S]*$/, '')
    .replace(/\$\{(?:encodeURIComponent|Uri\.encodeComponent)\([^)]*\)\}/g, '{}')
    .replace(/\$\{[^}]+\}/g, '{}').replace(/\$\w+/g, '{}').replace(/\{[^}]+\}/g, '{}')
    .replace('/api/v1.0/', '/api/v1/').toLowerCase();
}
for (const endpoint of endpoints) {
  endpoint.clients = clients.filter(call => call.method === endpoint.method && routeShape(call.route) === routeShape(endpoint.route));
}
for (const call of clients) {
  call.backendMatches = endpoints.filter(endpoint => endpoint.method === call.method && routeShape(endpoint.route) === routeShape(call.route)).map(endpoint => `${endpoint.controller}.${endpoint.action}`);
}

const api = {
  generatedAt: new Date().toISOString(), sourceRoot: root,
  summary: {
    backendEndpoints: endpoints.length,
    controllerFiles: new Set(endpoints.map(x => x.file)).size,
    controllerClassNames: new Set(endpoints.map(x => x.controller)).size,
    webCalls: clients.filter(x => x.client === 'web').length,
    mobileCalls: clients.filter(x => x.client === 'mobile').length,
    backendEndpointsUsedByWeb: endpoints.filter(x => x.clients.some(c => c.client === 'web')).length,
    backendEndpointsUsedByMobile: endpoints.filter(x => x.clients.some(c => c.client === 'mobile')).length,
    unmatchedClientCalls: clients.filter(x => !x.backendMatches.length).length,
  },
  endpoints, clients, schemas: Object.fromEntries([...schemas].sort(([a], [b]) => a.localeCompare(b))),
};

const outputDir = path.join(root, 'docs', 'api-audit');
fs.mkdirSync(outputDir, { recursive: true });
fs.writeFileSync(path.join(outputDir, 'api-inventory.json'), JSON.stringify(api, null, 2) + '\n');

const md = [];
md.push('# AqariOS — Full API Audit');
md.push('');
md.push(`> Generated from the current source tree on ${api.generatedAt.slice(0, 10)}. Backend routes come from controller attributes; Web/Mobile usage comes from production client source under \`frontend/src\` and \`mobile/lib\`. Tests and generated build files are excluded.`);
md.push('');
md.push('## Executive summary');
md.push('');
md.push(`- Backend: **${api.summary.backendEndpoints} endpoints** in **${api.summary.controllerFiles} controller files** (${api.summary.controllerClassNames} distinct class names; a few names repeat in different namespaces).`);
md.push(`- Web: **${api.summary.webCalls} HTTP call sites**, matching **${api.summary.backendEndpointsUsedByWeb} backend endpoints**.`);
md.push(`- Mobile: **${api.summary.mobileCalls} HTTP call sites**, matching **${api.summary.backendEndpointsUsedByMobile} backend endpoints**.`);
md.push(`- Client calls without an exact static route+method match: **${api.summary.unmatchedClientCalls}** (dynamic route construction and query strings can require manual confirmation).`);
md.push('- Standard authenticated calls use `Authorization: Bearer <JWT>`. Authentication refresh can also use the secure `refreshToken` cookie. Company scope is resolved server-side from JWT; `companyId` is not a normal tenant request-body field.');
md.push('- Standard errors use ASP.NET `ProblemDetails` / `ValidationProblemDetails`; common statuses are 400, 401, 403, 404, 409, 422, 429, and 500. Individual declared statuses are listed per endpoint.');
md.push('');
md.push('## How to read body and response types');
md.push('');
md.push('Each endpoint shows route/query/body inputs and declared response variants. Complex type fields are listed in the schema catalogue after the endpoint inventory. JSON property names follow ASP.NET camelCase serialization (for example `LeaseContractId` → `leaseContractId`). No global string-enum converter is configured in the current API, so enum request/response values use their numeric wire representation unless a type has its own converter.');
md.push('');
md.push('## Backend endpoints, file by file');
md.push('');
for (const [controller, items] of Map.groupBy(endpoints, x => x.controller)) {
  md.push(`### ${controller}`);
  md.push('');
  md.push(`Source: \`${items[0].file}\``);
  md.push('');
  for (const e of items) {
    md.push(`#### ${e.method} \`${e.route}\` — ${e.action}`);
    md.push('');
    md.push(`- Auth: \`${e.auth}\``);
    const pathParams = e.params.filter(p => p.binding === 'route');
    const queryParams = e.params.filter(p => p.binding === 'query' || p.binding === 'route-or-query');
    md.push(`- Route/query: ${[...pathParams, ...queryParams].length ? [...pathParams, ...queryParams].map(p => `\`${p.name}: ${p.type}${p.optional ? ' (optional)' : ''}\``).join(', ') : 'none'}`);
    md.push(`- Body: ${e.bodyType ? `\`${e.bodyType}\` (see schema catalogue)` : 'none'}`);
    md.push(`- Responses: ${e.responses.length ? e.responses.map(r => `\`${r.status}: ${r.type}\``).join(', ') : `controller return \`${e.returnType}\` (status inferred from implementation)`}`);
    md.push(`- Used by: ${e.clients.length ? e.clients.map(c => `${c.client} \`${c.file}:${c.line}\``).join('; ') : 'no exact production client call found'}`);
    md.push(`- Location: \`${e.file}:${e.line}\``);
    md.push('');
  }
}

md.push('## Request and response schema catalogue');
md.push('');
const referencedTypes = new Set();
const addTypeReferences = type => {
  for (const name of type.match(/[A-Za-z_]\w*/g) ?? []) if (schemas.has(name)) referencedTypes.add(name);
};
for (const e of endpoints) {
  if (e.bodyType) addTypeReferences(e.bodyType);
  for (const p of e.params) addTypeReferences(p.type ?? '');
  for (const r of e.responses) addTypeReferences(r.type);
}
for (const name of referencedTypes) {
  const schema = schemas.get(name);
  for (const field of schema?.fields ?? []) addTypeReferences(field.type);
}
for (const name of [...referencedTypes].sort()) {
  const schema = schemas.get(name);
  if (!schema) continue;
  md.push(`### ${name}`);
  md.push('');
  if (schema.kind === 'enum') md.push(`- Values: ${schema.values.map(v => `\`${v}\``).join(', ')}`);
  else md.push(`- Fields: ${schema.fields.length ? schema.fields.map(f => `\`${f.name}: ${f.type}\``).join(', ') : 'none'}`);
  md.push(`- Source: \`${schema.file}:${schema.line}\``);
  md.push('');
}

md.push('## Web API call sites, file by file');
md.push('');
for (const [file, items] of Map.groupBy(clients.filter(x => x.client === 'web'), x => x.file)) {
  md.push(`### \`${file}\``);
  md.push('');
  for (const c of items) md.push(`- ${c.method} \`${c.route}\` → \`${c.responseType}\`; backend match: ${c.backendMatches.length ? c.backendMatches.map(x => `\`${x}\``).join(', ') : '**not statically matched**'} (line ${c.line})`);
  md.push('');
}

md.push('## Mobile API call sites, file by file');
md.push('');
for (const [file, items] of Map.groupBy(clients.filter(x => x.client === 'mobile'), x => x.file)) {
  md.push(`### \`${file}\``);
  md.push('');
  for (const c of items) md.push(`- ${c.method} \`${c.route}\`; backend match: ${c.backendMatches.length ? c.backendMatches.map(x => `\`${x}\``).join(', ') : '**not statically matched**'} (line ${c.line})`);
  md.push('');
}

md.push('## Non-REST and special integrations');
md.push('');
md.push('- SignalR notifications hub: `/hubs/notifications`; bearer token may be supplied as the `access_token` query parameter for hub connections only.');
md.push('- File upload/download uses signed capability URLs. The binary upload body is raw bytes (`application/octet-stream`), and download returns a binary stream with attachment headers.');
md.push('- eFAWATEERcom callback is an inbound webhook protected by configured HMAC signing, not by a user JWT.');
md.push('- Development email/SMS/seed/test controllers are environment-gated and must not be treated as production APIs.');
md.push('- Health checks, OpenAPI JSON, Scalar UI, and Hangfire dashboard are operational surfaces configured in `Program.cs`, not business-client endpoints.');
md.push('');
md.push('## Outbound and internal service APIs');
md.push('');
md.push('### Resend (active email provider)');
md.push('');
md.push('- Request: `POST https://api.resend.com/emails`; header `Authorization: Bearer <Resend:ApiKey>`; JSON body `{ from: string, to: string[], subject: string, html?: string, text?: string }`.');
md.push('- Response handling: any HTTP success is mapped to internal `true`; failures are sanitized to provider error name/message and mapped to `false`. Raw provider bodies are not exposed to Web/Mobile.');
md.push('- Source: `src/PropertyOS.Infrastructure/Notifications/Services/ResendEmailSender.cs`.');
md.push('');
md.push('### Infobip (active SMS provider)');
md.push('');
md.push('- Request: `POST <Infobip:BaseUrl>/sms/3/messages`; header `Authorization: App <Infobip:ApiKey>`; JSON body `{ messages: [{ sender, destinations: [{ to }], content: { text } }] }`.');
md.push('- Response handling: requires HTTP success plus an accepted provider response; maps to internal `true/false`. Provider error types are sanitized and not returned to clients.');
md.push('- Source: `src/PropertyOS.Infrastructure/Notifications/Services/InfobipSmsSender.cs`.');
md.push('');
md.push('### Geoapify reverse geocoding');
md.push('');
md.push('- Request: `GET <Geocoding:BaseUrl>/v1/geocode/reverse?lat=<decimal>&lon=<decimal>&lang=ar|en&format=json&apiKey=<secret>`; no body.');
md.push('- Response consumed: first item in `results[]`; fields `country_code`, `country`, `state`, `city`, `county|district`, `suburb|neighbourhood`, `street`, `housenumber`, `postcode`, `formatted`. Failures map to `null`.');
md.push('- Source: `src/PropertyOS.Infrastructure/Locations/GeoapifyReverseGeocodingService.cs`.');
md.push('');
md.push('### Internal utility scraper');
md.push('');
md.push('- Request: `POST <ScraperService:BaseUrl>/internal/v1/electricity/bills` or `/internal/v1/water/bills`; JSON body `{ account_number: string }`.');
md.push('- Authentication headers: `X-AqariOS-Timestamp`, `X-AqariOS-Nonce`, `X-AqariOS-Signature`; signature is HMAC-SHA256 over timestamp, nonce, method, path, and SHA-256 body hash.');
md.push('- Response: `{ success: boolean, bills: [{ external_id, bill_date, due_date?, amount, status, reference?, currency? }], total_outstanding_balance?, error_code?, error_message? }`.');
md.push('- Source: `src/PropertyOS.Infrastructure/UtilityBills/Providers/InternalUtilityScraperClient.cs` and `.../Models/InternalScraperResponseModels.cs`.');
md.push('');
md.push('### Legacy/inactive provider implementations');
md.push('');
md.push('- Brevo email: `POST https://api.brevo.com/v3/smtp/email`, `api-key` header, JSON `{ sender: {name,email}, to: [{email,name}], subject, htmlContent }`; retained but not the active `IEmailSender`.');
md.push('- Twilio SMS: `POST https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json`, Basic auth, form body `To`, `MessagingServiceSid`, `Body`; retained but not the active `ISmsSender`.');
md.push('- eFAWATEERcom outbound gateway is currently a null/provider placeholder; the real outbound provider contract is not implemented. Its inbound webhook is included in the backend endpoint inventory.');
md.push('');
md.push('## Audit limitations');
md.push('');
md.push('- This is a static source audit. Runtime-only conventions, middleware-added responses, and dynamically concatenated client URLs may require a running integration environment to confirm.');
md.push('- `ProducesResponseType` is not present on every action. Where absent, the report retains the action return type or marks the response as unspecified.');
md.push('- The JSON companion is the complete machine-readable inventory and includes all extracted parameters, source locations, client matches, and parsed schemas.');

fs.writeFileSync(path.join(outputDir, 'FULL_API_REPORT.md'), md.join('\n') + '\n');
console.log(JSON.stringify(api.summary, null, 2));
