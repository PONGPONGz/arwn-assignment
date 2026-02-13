const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
const TENANT_ID = process.env.NEXT_PUBLIC_TENANT_ID || "a0000000-0000-0000-0000-000000000001";
const API_TOKEN = process.env.NEXT_PUBLIC_API_TOKEN || "admin-token-00000001";

interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, string[]>;
  };
}

export class ApiRequestError extends Error {
  code: string;
  status: number;
  details?: Record<string, string[]>;

  constructor(status: number, body: ApiError) {
    super(body.error.message);
    this.code = body.error.code;
    this.status = status;
    this.details = body.error.details;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Tenant-Id": TENANT_ID,
      "Authorization": `Bearer ${API_TOKEN}`,
      ...options.headers,
    },
  });

  if (!res.ok) {
    let body: ApiError;
    try {
      body = await res.json() as ApiError;
    } catch {
      body = { error: { code: "HTTP_ERROR", message: `Request failed with status ${res.status}` } };
    }
    throw new ApiRequestError(res.status, body);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }),
};
