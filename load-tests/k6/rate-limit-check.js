import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5235').replace(/\/+$/, '');

const responses200 = new Counter('responses_200');
const responses429 = new Counter('responses_429');
const unexpected = new Counter('responses_unexpected');

export const options = {
  summaryTrendStats: ['avg', 'med', 'min', 'max', 'p(90)', 'p(95)', 'p(99)'],
  scenarios: {
    rate_limit_probe: {
      executor: 'constant-vus',
      vus: 5,
      duration: '10s',
      gracefulStop: '2s',
    },
  },

  thresholds: {
    responses_429: ['count>0'],
    responses_unexpected: ['count==0'],
  },
};

export default function () {
  const res = http.get(`${BASE_URL}/api/v1/marketplace/listings`);

  if (res.status === 200) {
    responses200.add(1);
    return;
  }

  if (res.status === 429) {
    responses429.add(1);

    check(res, {
      '429 has Retry-After': (r) =>
        r.headers['Retry-After'] !== undefined,
    });

    return;
  }

  unexpected.add(1);

  check(res, {
    'response is 200 or 429': (r) =>
      r.status === 200 || r.status === 429,
  });
}
