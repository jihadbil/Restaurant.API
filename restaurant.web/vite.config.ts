import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 52770,
        proxy: {
            '/api': {
                target: 'https://localhost:7040',
                changeOrigin: true,
                secure: false,
            },
            '/images': {
                target: 'https://localhost:7040',
                changeOrigin: true,
                secure: false,
            },
        },
    },
});
