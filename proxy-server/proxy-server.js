const express = require('express');
const cors = require('cors');
const { createProxyMiddleware } = require('http-proxy-middleware');
const dotenv = require('dotenv');
const path = require('path');

dotenv.config({ path: path.join(__dirname, 'proxy-server.env') });

const app = express();

const PORT = process.env.PROXY_PORT || 3001;
const TARGET = process.env.API_URL || 'http://localhost:5079';
const CLIENT_ORIGIN = process.env.REACT_APP_URL || 'http://localhost:3000';

app.use(cors({
    origin: CLIENT_ORIGIN,
    credentials: true
}));

app.use((req, res, next) => {
    console.log(`[INCOMING] ${req.method} ${req.originalUrl}`);
    next();
});

app.use('/api', (req, res, next) => {
    res.header("Access-Control-Allow-Origin", CLIENT_ORIGIN);
    res.header("Access-Control-Allow-Credentials", "true");
    res.header("Access-Control-Allow-Methods", "GET,HEAD,PUT,PATCH,POST,DELETE");
    res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization");
    if (req.method === "OPTIONS") {
        return res.sendStatus(204);
    }
    next();
});


app.use('/api', createProxyMiddleware({
    target: TARGET,
    changeOrigin: true,
    secure: false,
    pathRewrite: { '^/api': '/api' },
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[PROXY REQ] ${req.method} ${req.path}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`[PROXY RES] ${req.method} ${req.path} -> ${proxyRes.statusCode}`);
    },
    onError(err, req, res) {
        console.error('[PROXY ERROR]', err.message);
        res.status(500).send('Proxy error');
    }
}));

app.listen(PORT, () => {
    console.log(`🔁 Proxy server listening on http://localhost:${PORT}, forwarding to ${TARGET}`);
});
