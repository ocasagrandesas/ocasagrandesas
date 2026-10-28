const REQUEST_TIMEOUT_MS = 10000;
const CSRF_HEADER = "X-CSRF-TOKEN";

export class ApiError extends Error {
  constructor(status, title) {
    super(title ?? `A API respondeu com o status ${status}.`);
    this.name = "ApiError";
    this.status = status;
  }
}

async function request(method, path, { body, csrfToken } = {}) {
  const headers = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (csrfToken) headers[CSRF_HEADER] = csrfToken;

  const response = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: "same-origin",
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(response.status, problem?.title);
  }

  return response.status === 204 ? null : response.json();
}

// The token is bound to the signed-in identity, so callers fetch a fresh one after login.
export async function getCsrfToken() {
  const { token } = await request("GET", "/api/auth/csrf");
  return token;
}

export const getJson = (path) => request("GET", path);
export const postJson = (path, body, csrfToken) => request("POST", path, { body, csrfToken });
export const putJson = (path, body, csrfToken) => request("PUT", path, { body, csrfToken });
