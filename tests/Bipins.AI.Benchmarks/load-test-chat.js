// k6 load test for chat endpoint
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const chatDuration = new Trend('chat_duration');

export const options = {
  stages: [
    { duration: '30s', target: 5 },    // Ramp up to 5 users
    { duration: '1m', target: 5 },    // Stay at 5 users
    { duration: '30s', target: 10 }, // Ramp up to 10 users
    { duration: '1m', target: 10 },   // Stay at 10 users
    { duration: '30s', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<5000'], // 95% of requests should be below 5s
    'errors': ['rate<0.1'],              // Error rate should be less than 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const API_KEY = __ENV.API_KEY || 'test-key';

export default function () {
  const payload = JSON.stringify({
    tenantId: `tenant_${__VU}`,
    correlationId: `corr_${__VU}_${__ITER}`,
    inputType: 'chat',
    payload: {
      messages: [
        {
          role: 'User',
          content: 'What is machine learning?',
        },
      ],
    },
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Basic ${btoa(`user:pass`)}`,
      'X-API-Key': API_KEY,
    },
  };

  const startTime = Date.now();
  const res = http.post(`${BASE_URL}/v1/chat`, payload, params);
  const duration = Date.now() - startTime;

  chatDuration.add(duration);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 5s': (r) => r.timings.duration < 5000,
    'has content': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.data && body.data.content;
      } catch {
        return false;
      }
    },
  });

  if (!success) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }

  sleep(2); // Wait 2 seconds between requests
}
