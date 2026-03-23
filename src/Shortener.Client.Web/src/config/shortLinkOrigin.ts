/**
 * Public origin where short codes resolve (same host as the API redirect route).
 * Override with VITE_SHORT_LINK_ORIGIN for production (e.g. https://go.example.com).
 */
export function getShortLinkOrigin(): string {
  const fromEnv = import.meta.env.VITE_SHORT_LINK_ORIGIN?.trim();
  if (fromEnv) {
    return fromEnv.replace(/\/$/, '');
  }
  if (import.meta.env.DEV) {
    return 'http://localhost:5175';
  }
  return typeof window !== 'undefined' ? window.location.origin : '';
}
