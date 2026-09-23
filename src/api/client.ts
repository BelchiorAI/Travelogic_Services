import { ApiError } from "./errors";
import {
  mockCreateSupplier,
  mockExtractSupplier,
  mockGetFeatures,
  mockGetSupplier,
  mockGetSuppliers,
} from "./mock";
import type {
  CreateSupplierRequest,
  ExtractSupplierResponse,
  FeatureFlags,
  PagedResult,
  ProblemDetails,
  Supplier,
  SupplierListParams,
  SupplierSummary,
} from "./types";

export { ApiError };

export const API_BASE_URL =
  import.meta.env["VITE_API_BASE_URL"] ?? "http://localhost:5000";

export const USE_MOCKS =
  (import.meta.env["VITE_USE_MOCKS"] ?? "true").toString() === "true";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch {
    throw new ApiError(0, "Network error", {
      title: "Network error",
      detail: "Could not reach the Supplier Hub API.",
    });
  }

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }
    throw new ApiError(
      response.status,
      problem?.title || response.statusText || "Request failed",
      problem,
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

function buildQuery(params: SupplierListParams): string {
  const query = new URLSearchParams();
  query.set("page", String(params.page ?? 1));
  query.set("pageSize", String(params.pageSize ?? 12));
  if (params.search) query.set("search", params.search);
  if (params.type) query.set("type", params.type);
  return query.toString();
}

export function getSuppliers(
  params: SupplierListParams,
): Promise<PagedResult<SupplierSummary>> {
  if (USE_MOCKS) return mockGetSuppliers(params);
  return request(`/api/v1/suppliers?${buildQuery(params)}`);
}

export function getSupplier(id: string): Promise<Supplier> {
  if (USE_MOCKS) return mockGetSupplier(id);
  return request(`/api/v1/suppliers/${encodeURIComponent(id)}`);
}

export function createSupplier(
  body: CreateSupplierRequest,
): Promise<Supplier> {
  if (USE_MOCKS) return mockCreateSupplier(body);
  return request(`/api/v1/suppliers`, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function getFeatures(): Promise<FeatureFlags> {
  if (USE_MOCKS) return mockGetFeatures();
  return request(`/api/v1/features`);
}

export function extractSupplier(
  text: string,
): Promise<ExtractSupplierResponse> {
  if (USE_MOCKS) return mockExtractSupplier(text);
  return request(`/api/v1/suppliers/extract`, {
    method: "POST",
    body: JSON.stringify({ text }),
  });
}
