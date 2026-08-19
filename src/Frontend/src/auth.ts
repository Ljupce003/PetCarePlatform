import type { Session } from './types';

const config = {
  baseUrl: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'petcare',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'petcare-demo'
};
const sessionKey = 'petcare.session';
const verifierKey = 'petcare.pkce.verifier';
const stateKey = 'petcare.pkce.state';
const issuer = `${config.baseUrl}/realms/${config.realm}`;
export const accountSettingsUrl = () => `${issuer}/account`;

function base64Url(bytes: Uint8Array) {
  return btoa(String.fromCharCode(...bytes)).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '');
}

function decodeJwt(token: string): Record<string, unknown> {
  const encoded = token.split('.')[1];
  if (!encoded) throw new Error('The access token is malformed.');
  return JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(encoded.replaceAll('-', '+').replaceAll('_', '/')), char => char.charCodeAt(0))));
}

function sessionFromToken(accessToken: string): Session {
  const claims = decodeJwt(accessToken);
  const realmAccess = claims.realm_access as { roles?: string[] } | undefined;
  return {
    accessToken,
    subject: String(claims.sub ?? ''),
    username: String(claims.preferred_username ?? claims.name ?? 'PetCare user'),
    roles: realmAccess?.roles ?? [],
    expiresAt: Number(claims.exp ?? 0) * 1000
  };
}

export function getSession(): Session | null {
  const raw = sessionStorage.getItem(sessionKey);
  if (!raw) return null;
  try {
    const session = JSON.parse(raw) as Session;
    return session.subject && session.expiresAt > Date.now() + 10_000 ? session : null;
  } catch { return null; }
}

export async function redirectToLogin() {
  const verifier = base64Url(crypto.getRandomValues(new Uint8Array(48)));
  const challenge = base64Url(new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier))));
  const state = crypto.randomUUID();
  sessionStorage.setItem(verifierKey, verifier);
  sessionStorage.setItem(stateKey, state);
  const params = new URLSearchParams({
    client_id: config.clientId,
    response_type: 'code',
    scope: 'openid profile email',
    redirect_uri: window.location.origin + window.location.pathname,
    code_challenge: challenge,
    code_challenge_method: 'S256',
    state
  });
  window.location.assign(`${issuer}/protocol/openid-connect/auth?${params}`);
}

export async function completeLogin(): Promise<Session | null> {
  const url = new URL(window.location.href);
  const code = url.searchParams.get('code');
  if (!code) return getSession();
  if (url.searchParams.get('state') !== sessionStorage.getItem(stateKey)) throw new Error('The Keycloak login response did not match this browser session.');
  const verifier = sessionStorage.getItem(verifierKey);
  if (!verifier) throw new Error('The login challenge has expired. Please sign in again.');
  const body = new URLSearchParams({ grant_type: 'authorization_code', client_id: config.clientId, code, redirect_uri: window.location.origin + window.location.pathname, code_verifier: verifier });
  const response = await fetch(`${issuer}/protocol/openid-connect/token`, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body });
  if (!response.ok) throw new Error('Keycloak could not exchange the login code.');
  const result = await response.json() as { access_token: string };
  const session = sessionFromToken(result.access_token);
  sessionStorage.setItem(sessionKey, JSON.stringify(session));
  sessionStorage.removeItem(verifierKey); sessionStorage.removeItem(stateKey);
  window.history.replaceState({}, document.title, window.location.pathname);
  return session;
}

export function logout() {
  sessionStorage.removeItem(sessionKey);
  const params = new URLSearchParams({ client_id: config.clientId, post_logout_redirect_uri: window.location.origin + window.location.pathname });
  window.location.assign(`${issuer}/protocol/openid-connect/logout?${params}`);
}
