import { ApiError } from "./errors";
import {
  mockCreateSupplier,
  mockDeleteSupplierMedia,
  mockUploadSupplierMedia,
  mockExtractSupplier,
  mockGetFeatures,
  mockGetSupplier,
  mockGetSuppliers,
} from "./mock";
import type {
  CreateSupplierRequest,
  ExtractSupplierResponse,
  FeatureFlags,
  Media,
  MediaUploadTicket,
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

/** Media URLs from the API are relative to it; mock URLs are already absolute (blob: or https:). */
export function mediaUrl(url: string): string {
  return /^(https?:|blob:|data:)/.test(url) ? url : `${API_BASE_URL}${url}`;
}

/** Upload progress, 0 to 1. */
export type UploadProgress = (fraction: number) => void;

/**
 * Uploads a photo or video in three steps, so the file goes straight to S3 and never through the API:
 * 1. ask the API for a signed upload URL, 2. send the file to it, 3. confirm, which saves the reference.
 */
export async function uploadSupplierMedia(
  supplierId: string,
  file: File,
  onProgress?: UploadProgress,
): Promise<Media> {
  if (USE_MOCKS) return mockUploadSupplierMedia(supplierId, file, onProgress);
  const base = `/api/v1/suppliers/${encodeURIComponent(supplierId)}/media`;

  const ticket = await request<MediaUploadTicket>(`${base}/uploads`, {
    method: "POST",
    body: JSON.stringify({ fileName: file.name, contentType: file.type, sizeBytes: file.size }),
  });

  await sendFile(ticket, file, onProgress);

  return request<Media>(base, {
    method: "POST",
    body: JSON.stringify({ mediaId: ticket.mediaId, fileName: file.name, contentType: file.type }),
  });
}

/** XMLHttpRequest rather than fetch, because only it reports upload progress. */
function sendFile(ticket: MediaUploadTicket, file: File, onProgress?: UploadProgress): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open(ticket.method, ticket.uploadUrl);
    for (const [name, value] of Object.entries(ticket.headers)) xhr.setRequestHeader(name, value);
    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(event.loaded / event.total);
    };
    xhr.onload = () =>
      xhr.status >= 200 && xhr.status < 300
        ? resolve()
        : reject(
            new ApiError(xhr.status, "Upload failed", {
              title: "Upload failed",
              detail: "The file could not be uploaded to storage. Please try again.",
            }),
          );
    xhr.onerror = () =>
      reject(
        new ApiError(0, "Network error", {
          title: "Network error",
          detail: "Could not reach file storage. Check your connection and try again.",
        }),
      );
    xhr.send(file);
  });
}

export function deleteSupplierMedia(supplierId: string, mediaId: string): Promise<void> {
  if (USE_MOCKS) return mockDeleteSupplierMedia(supplierId, mediaId);
  return request(
    `/api/v1/suppliers/${encodeURIComponent(supplierId)}/media/${encodeURIComponent(mediaId)}`,
    { method: "DELETE" },
  );
}
