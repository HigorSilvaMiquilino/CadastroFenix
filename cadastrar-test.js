import http from 'k6/http';
import { check, sleep, Counter } from 'k6';
import { SharedArray } from 'k6/data';

const rateLimitedCounter = new Counter('rate_limited_requests'); 
const users = new SharedArray('users', function () {
    return JSON.parse(open('./usuario.json'));
});

export const options = {
    stages: [
        { duration: '1m', target: 10 },
        { duration: '3m', target: 100 },
        { duration: '1m', target: 0 },
    ],
    thresholds: {
        'http_req_duration': ['p(95)<500'],
        'http_req_failed': ['rate<0.01'],
        'rate_limited_requests': ['count<1000'],
    },
};

export default function () {
    const user = users[Math.floor(Math.random() * users.length)];
    const payload = JSON.stringify(user);
    const headers = { 'Content-Type': 'application/json' };

    const res = http.post('https://localhost:7011/api/v1/Cadastro/test-sliding-limiter', payload, { headers });

    if (res.status === 429) {
        rateLimitedCounter.add(1);
    }

    check(res, {
        'status is 200 or 429': (r) => r.status === 200 || r.status === 429,
        'status is 200 (successful registration)': (r) => r.status === 200,
    });

    sleep(Math.random() * 4 + 1);
}