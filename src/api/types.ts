export type SupplierType =
  | "Accommodation"
  | "Activity"
  | "Transport"
  | "Restaurant"
  | "Other";

export type ServiceType =
  | "Accommodation"
  | "Activity"
  | "Tour"
  | "Transfer"
  | "Meal"
  | "Other";

export type PricingUnit =
  | "PerPerson"
  | "PerPersonPerNight"
  | "PerRoomPerNight"
  | "PerVehicle"
  | "PerGroup";

export interface SupplierSummary {
  id: string;
  name: string;
  type: SupplierType;
  city: string;
  country: string;
  email: string;
  serviceCount: number;
  createdAt: string;
}

export interface Service {
  id: string;
  name: string;
  description: string | null;
  type: ServiceType;
  price: number;
  currency: string;
  pricingUnit: PricingUnit;
  durationMinutes: number | null;
  capacity: number | null;
  isActive: boolean;
}

export interface Supplier {
  id: string;
  name: string;
  type: SupplierType;
  email: string;
  phone: string | null;
  website: string | null;
  addressLine: string | null;
  city: string;
  country: string;
  createdAt: string;
  updatedAt: string;
  services: Service[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

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

export interface FeatureFlags {
  aiExtraction: boolean;
}

export interface ExtractSupplierResponse {
  draft: CreateSupplierRequest;
  warnings: string[];
}

export interface ProblemDetails {
  type?: string | undefined;
  title?: string | undefined;
  status?: number | undefined;
  detail?: string | undefined;
  errors?: Record<string, string[]> | undefined;
}

export interface SupplierListParams {
  page?: number | undefined;
  pageSize?: number | undefined;
  search?: string | undefined;
  type?: SupplierType | "" | undefined;
}

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
