/**
 * API types. Response shapes come straight from the generated OpenAPI types (schema.gen.ts), so
 * they can't drift from the Supplier API. Regenerate with `npm run generate:api` while the API runs.
 */
import type { components } from "./schema.gen";

type Schemas = components["schemas"];

export type SupplierType = Schemas["SupplierType"];
export type ServiceType = Schemas["ServiceType"];
export type PricingUnit = Schemas["PricingUnit"];

export type SupplierSummary = Schemas["SupplierSummaryDto"];
export type Service = Schemas["ServiceDto"];
export type Supplier = Schemas["SupplierDto"];

export type MediaKind = Schemas["MediaKind"];
/** A photo or video on a supplier's profile. `url` is relative to the API (see mediaUrl in client.ts). */
export type Media = Schemas["MediaDto"];
export type MediaUploadTicket = Schemas["MediaUploadTicket"];

/** The API always sends totalPages; OpenAPI marks it optional only because it is computed. */
export type PagedResult<T> = Omit<Schemas["PagedResultOfSupplierSummaryDto"], "items" | "totalPages"> & {
  items: T[];
  totalPages: number;
};

export type FeatureFlags = Schemas["FeaturesResponse"];

/** An AI draft: the create-request shape, where anything the text didn't state is null. */
export type SupplierDraft = Schemas["CreateSupplierRequest"];

/** `field` uses the validation-error key format (e.g. "Services[0].Price"); "" means the whole draft. */
export type ExtractionWarning = Schemas["ExtractionWarning"];
export type ExtractSupplierResponse = Schemas["ExtractSupplierDraftResult"];

export type ProblemDetails = Schemas["HttpValidationProblemDetails"];

/**
 * What the form sends. Stricter than the API's request type (the form always supplies these
 * fields); the checks below make the build fail if it uses a field name the API doesn't know.
 */
export interface CreateServiceRequest {
  name: string;
  description?: string | undefined;
  type: ServiceType;
  price: number;
  currency: string;
  pricingUnit: PricingUnit;
  durationMinutes?: number | undefined;
  capacity?: number | undefined;
}

export interface CreateSupplierRequest {
  name: string;
  type: SupplierType;
  email: string;
  phone?: string | undefined;
  website?: string | undefined;
  addressLine?: string | undefined;
  city: string;
  country: string;
  services: CreateServiceRequest[];
}

type OnlyKnownKeys<Local, Api> = Exclude<keyof Local, keyof Api> extends never ? true : never;
const requestFieldsMatchApi: [
  OnlyKnownKeys<CreateSupplierRequest, Schemas["CreateSupplierRequest"]>,
  OnlyKnownKeys<CreateServiceRequest, Schemas["CreateServiceRequest"]>,
] = [true, true];
void requestFieldsMatchApi;

export interface SupplierListParams {
  page?: number | undefined;
  pageSize?: number | undefined;
  search?: string | undefined;
  type?: SupplierType | "" | undefined;
}

/** Mirrors the API's rules so the browser can reject a file before uploading it. */
export const MEDIA_RULES = {
  accept: "image/jpeg,image/png,image/webp,video/mp4,video/webm",
  maxImageBytes: 10 * 1024 * 1024,
  maxVideoBytes: 100 * 1024 * 1024,
  maxPerSupplier: 20,
} as const;

export const SUPPLIER_TYPES: SupplierType[] = [
  "Accommodation",
  "Activity",
  "Transport",
  "Restaurant",
  "Other",
];

export const SERVICE_TYPES: ServiceType[] = [
  "Accommodation",
  "Activity",
  "Tour",
  "Transfer",
  "Meal",
  "Other",
];

export const PRICING_UNITS: PricingUnit[] = [
  "PerPerson",
  "PerPersonPerNight",
  "PerRoomPerNight",
  "PerVehicle",
  "PerGroup",
];
