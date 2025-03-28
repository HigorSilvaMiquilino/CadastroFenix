import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

const users = new SharedArray('users', function () {
    return JSON.parse(open('./usuario.json'));
});

export const options = {
    stages: [
        { duration: '1m', target: 20 },
        { duration: '3m', target: 200 },
        { duration: '1m', target: 0 },
    ],
    thresholds: {
        'http_req_duration': ['p(95)<500'],
        'http_req_failed': ['rate<0.01'],
    },
};

export default function () {
    const user = users[Math.floor(Math.random() * users.length)];
    const res = http.get(`https://localhost:7011/api/v1/cadastro/verificar-email?email=${encodeURIComponent(user.email)}&confirmacao=${encodeURIComponent(user.email)}`);

    check(res, {
        'status is 200 or 429': (r) => r.status === 200 || r.status === 429,
        'status is 200 (successful verification)': (r) => r.status === 200,
    });

    sleep(Math.random() * 1.5 + 0.5);
}