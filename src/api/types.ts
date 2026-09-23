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
  description?: string;
  type: ServiceType;
  price: number;
  currency: string;
  pricingUnit: PricingUnit;
  durationMinutes?: number;
  capacity?: number;
}

export interface CreateSupplierRequest {
  name: string;
  type: SupplierType;
  email: string;
  phone?: string;
  website?: string;
  addressLine?: string;
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
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface SupplierListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  type?: SupplierType | "";
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
