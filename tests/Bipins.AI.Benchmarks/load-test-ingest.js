// k6 load test for ingestion endpoint
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const ingestDuration = new Trend('ingest_duration');

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up to 10 users
    { duration: '1m', target: 10 },    // Stay at 10 users
    { duration: '30s', target: 20 }, // Ramp up to 20 users
    { duration: '1m', target: 20 },   // Stay at 20 users
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<2000'], // 95% of requests should be below 2s
    'errors': ['rate<0.1'],              // Error rate should be less than 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const API_KEY = __ENV.API_KEY || 'test-key';

export default function () {
  const payload = JSON.stringify({
    tenantId: `tenant_${__VU}`,
    docId: `doc_${__VU}_${__ITER}`,
    text: generateText(1000), // 1KB text
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Basic ${btoa(`user:pass`)}`,
      'X-API-Key': API_KEY,
    },
  };

  const startTime = Date.now();
  const res = http.post(`${BASE_URL}/v1/ingest/text`, payload, params);
  const duration = Date.now() - startTime;

  ingestDuration.add(duration);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 2s': (r) => r.timings.duration < 2000,
  });

  if (!success) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }

  sleep(1);
}

function generateText(length) {
  const words = ['the', 'quick', 'brown', 'fox', 'jumps', 'over', 'lazy', 'dog'];
  let text = '';
  for (let i = 0; i < length; i++) {
    text += words[Math.floor(Math.random() * words.length)] + ' ';
  }
  return text.substring(0, length);
}
