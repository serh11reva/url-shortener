import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { fileURLToPath, URL } from 'node:url';

const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5175';

const proxyToHttps = apiTarget.startsWith('https:');

function parsePort(value: string | undefined, fallback: number): number {
  if (!value) {
    return fallback;
  }
  const n = Number(value);
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

/** Aspire sets PORT when the app is run via AddViteApp. */
const devServerPort = parsePort(process.env.PORT, 5173);

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: devServerPort,
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        // Dev ASP.NET Core uses a local dev certificate; allow TLS from the proxy.
        secure: !proxyToHttps,
        // If the API still returns a redirect, follow it here so the browser stays same-origin to Vite.
        followRedirects: true,
      },
    },
  },
});
