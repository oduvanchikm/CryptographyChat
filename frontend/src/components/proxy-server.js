const express = require('express');
const cors = require('cors');
const { createProxyMiddleware } = require('http-proxy-middleware');
const dotenv = require('dotenv');

dotenv.config({ path: __dirname + '/proxy-server.env' });

const app = express();

app.use(cors({
    origin: process.env.REACT_APP_URL || 'http://localhost:3000',
    credentials: true
}));

const apiProxy = createProxyMiddleware({
    target: process.env.API_URL || 'http://localhost:5079',
    changeOrigin: true,
    secure: false,
    onProxyReq: (proxyReq, req, res) => {
        console.log(`[PROXY] ${req.method} ${req.path}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`[PROXY] ${req.method} ${req.path} -> ${proxyRes.statusCode}`);
    }
});

app.use('/api', apiProxy);

const PORT = process.env.PROXY_PORT || 3001;
app.listen(PORT, () => {
    console.log(`Proxy server running on port ${PORT}`);
});