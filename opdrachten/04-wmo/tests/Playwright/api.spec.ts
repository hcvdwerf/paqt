import { test, expect, request, APIRequestContext } from '@playwright/test';
import { readFileSync } from 'node:fs';

const resident = (n: number) => `30000000-0000-0000-0000-${String(n).padStart(12, '0')}`;
const companyA = '10000000-0000-0000-0000-000000000001';
const companyB = '10000000-0000-0000-0000-000000000002';
const contexts: APIRequestContext[] = [];
async function client(name: string) {
  const auth = await request.newContext();
  let response: Awaited<ReturnType<typeof auth.post>>;
  for (let attempt = 0; attempt < 6; attempt++) {
    response = await auth.post(process.env.TOKEN_URL!, {
      form: { grant_type: 'client_credentials', client_id: name, client_secret: readFileSync(`/secrets/${name}-secret`, 'utf8').trim() }
    });
    if (response.status() < 500 || attempt === 5) break;
    await response.dispose();
    await new Promise(resolve => setTimeout(resolve, 250 * (attempt + 1)));
  }
  expect(response.status(), 'Keycloak tokenuitgifte').toBe(200);
  const { access_token } = await response.json();
  await auth.dispose();
  const api = await request.newContext({ baseURL: process.env.BASE_URL, extraHTTPHeaders: { Authorization: `Bearer ${access_token}` } });
  contexts.push(api);
  return api;
}
function booking(distanceKilometers = 1) {
  return { departureAt: new Date(Date.now() + 3600000).toISOString(), destinationAddress: 'Fictief reisplein 10, Utrecht', distanceKilometers };
}
async function balance(api: APIRequestContext, id: string) {
  const response = await api.get('/api/v1/residents');
  expect(response.status()).toBe(200);
  return (await response.json()).items.find((r: any) => r.id === id).budget.remainingKilometers;
}
test.beforeAll(async () => {
  const api = await request.newContext({ baseURL: process.env.BASE_URL });
  await expect.poll(async () => { try { return (await api.get('/health/ready')).status(); } catch { return 0; } }, { timeout: 60000 }).toBe(200);
  await api.dispose();
});
test.afterAll(async () => { for (const api of contexts) await api.dispose(); });

test('callcenter bereikt alle Utrechtse bewoners via paginering', async () => {
  const api = await client('callcenter');
  const ids: string[] = [];
  for (let page = 1; page <= 3; page++) {
    const response = await api.get(`/api/v1/residents?page=${page}&pageSize=2`);
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.totalCount).toBe(6);
    ids.push(...body.items.map((r: any) => r.id));
  }
  expect(new Set(ids).size).toBe(6);
  expect(ids).not.toContain(resident(7));
});

test('boeken boekt af en toont de rit uitsluitend aan de juiste vervoerder', async () => {
  const callcenter = await client('callcenter');
  const a = await client('carrier-a');
  const b = await client('carrier-b');
  const before = await balance(callcenter, resident(2));
  const response = await callcenter.post(`/api/v1/residents/${resident(2)}/trips`, { data: booking(2.125) });
  expect(response.status()).toBe(201);
  const trip = await response.json();
  expect(trip.taxiCompanyId).toBe(companyB);
  expect(await balance(callcenter, resident(2))).toBe(before - 2.125);
  const own = await b.get('/api/v1/trips');
  expect((await own.json()).items.map((t: any) => t.id)).toContain(trip.id);
  const other = await a.get(`/api/v1/trips?taxiCompanyId=${companyB}`);
  const otherTrips = (await other.json()).items;
  expect(otherTrips.every((t: any) => t.taxiCompanyId === companyA)).toBe(true);
  expect(otherTrips.map((t: any) => t.id)).not.toContain(trip.id);
});

test('onvoldoende saldo en gelijktijdige boekingen behouden consistent budget', async () => {
  const api = await client('callcenter');
  const before = await balance(api, resident(3));
  const responses = await Promise.all([1, 2].map(() => api.post(`/api/v1/residents/${resident(3)}/trips`, { data: booking(3) })));
  if (before >= 3) {
    expect(responses.filter(r => r.status() === 201)).toHaveLength(1);
    expect(await balance(api, resident(3))).toBe(before - 3);
  } else {
    expect(responses.every(r => r.status() === 409)).toBe(true);
    expect(await balance(api, resident(3))).toBe(before);
  }
  const rejected = responses.find(r => r.status() === 409)!;
  expect((await rejected.json()).code).toBe('insufficient_budget');
});

test('achterstallige reset wordt ingehaald en herhaling vult verbruik niet opnieuw aan', async () => {
  const api = await client('callcenter');
  const renewal = await client('renewal');
  expect((await renewal.post('/api/v1/budget-renewals')).status()).toBe(200);
  const response = await api.get('/api/v1/residents');
  const permit = (await response.json()).items.find((r: any) => r.id === resident(5)).budget;
  expect(new Date(permit.periodEnd).getTime()).toBeGreaterThan(Date.now());
  const before = await balance(api, resident(5));
  expect((await api.post(`/api/v1/residents/${resident(5)}/trips`, { data: booking(1) })).status()).toBe(201);
  for (let i = 0; i < 2; i++) expect((await renewal.post('/api/v1/budget-renewals')).status()).toBe(200);
  expect(await balance(api, resident(5))).toBe(before - 1);
});

test('tokenvalidatie, rollen en bedrijfsscope worden afgedwongen', async () => {
  const anonymous = await request.newContext({ baseURL: process.env.BASE_URL });
  expect((await anonymous.get('/api/v1/residents')).status()).toBe(401);
  expect((await anonymous.get('/api/v1/residents', { headers: { Authorization: 'Bearer invalid' } })).status()).toBe(401);
  await anonymous.dispose();
  expect((await (await client('bad-audience')).get('/api/v1/residents')).status()).toBe(401);
  expect((await (await client('unauthorized')).get('/api/v1/residents')).status()).toBe(403);
  expect((await (await client('carrier-missing-company')).get('/api/v1/trips')).status()).toBe(403);
  const carrier = await client('carrier-a');
  expect((await carrier.get('/api/v1/residents')).status()).toBe(403);
  expect((await carrier.post(`/api/v1/residents/${resident(1)}/trips`, { data: booking() })).status()).toBe(403);
  expect((await (await client('callcenter')).post('/api/v1/budget-renewals')).status()).toBe(403);
});

test('invoerfouten zijn veilig en correleerbaar', async () => {
  const api = await client('callcenter');
  for (const data of [booking(0), booking(1.1234), { ...booking(), taxiCompanyId: companyB }, { ...booking(), departureAt: '2026-10-17T12:00:00' }]) {
    const response = await api.post(`/api/v1/residents/${resident(1)}/trips`, { data });
    expect(response.status()).toBe(400);
    const body = await response.json();
    expect(body.code).toBe('invalid_request');
    expect(body.correlationId).toBe(response.headers()['x-correlation-id']);
    expect(JSON.stringify(body)).not.toMatch(/stackTrace|password|Npgsql/);
  }
  expect((await api.get('/api/v1/residents?pageSize=101')).status()).toBe(400);
  expect((await api.post(`/api/v1/residents/${resident(4)}/trips`, { data: booking() })).status()).toBe(409);
  expect((await api.post(`/api/v1/residents/${resident(6)}/trips`, { data: booking() })).status()).toBe(409);
  expect((await api.post(`/api/v1/residents/${resident(7)}/trips`, { data: booking() })).status()).toBe(404);
});

test('Kong weigert te grote verzoeken en onbekende routes', async () => {
  const api = await client('callcenter');
  expect((await api.post(`/api/v1/residents/${resident(1)}/trips`, { data: { ...booking(), destinationAddress: 'x'.repeat(70000) } })).status()).toBe(413);
  expect((await api.get('/unknown')).status()).toBe(404);
});

test('standaard Swagger UI laadt het publieke Bearer-contract en beschermt API-handelingen via Kong', async () => {
  const anonymous = await request.newContext({ baseURL: process.env.BASE_URL });
  const shell = await anonymous.get('/swagger/index.html');
  expect(shell.status()).toBe(200);
  expect(await shell.text()).toContain('Swagger UI');
  const config = await anonymous.get('/swagger/index.js');
  expect(config.status()).toBe(200);
  expect(await config.text()).toContain('/openapi/v1.json');
  expect((await anonymous.get('/swagger/swagger-ui-bundle.js')).status()).toBe(200);
  expect((await anonymous.get('/swagger/swagger-ui.css')).status()).toBe(200);
  const response = await anonymous.get('/openapi/v1.json');
  expect(response.status()).toBe(200);
  const contract = await response.json();
  expect(contract.servers).toEqual([{ url: '/' }]);
  expect(contract.components.securitySchemes.Bearer.type).toBe('http');
  expect(contract.components.securitySchemes.Bearer.scheme).toBe('bearer');
  expect(Object.keys(contract.paths)).toHaveLength(4);
  for (const path of Object.values(contract.paths) as any[]) {
    for (const operation of Object.values(path) as any[]) {
      expect(operation.security).toContainEqual({ Bearer: [] });
    }
  }
  expect((await anonymous.get('/api/v1/residents')).status()).toBe(401);
  await anonymous.dispose();
});

test('Kong begrenst het aantal verzoeken met HTTP 429', async () => {
  const api = await request.newContext({ baseURL: process.env.BASE_URL });
  let limited = false;
  // Meer dan twee vensters capaciteit: ook bij een minuutovergang deterministisch.
  for (let batch = 0; batch < 13 && !limited; batch++) {
    const responses = await Promise.all(Array.from({ length: 100 }, () => api.get('/health/live')));
    limited = responses.some(response => response.status() === 429);
    for (const response of responses) await response.dispose();
  }
  await api.dispose();
  expect(limited).toBe(true);
});
