import { z } from "zod";

import {
  PRICING_UNITS,
  SERVICE_TYPES,
  SUPPLIER_TYPES,
  type CreateSupplierRequest,
} from "@/api/types";

const supplierTypeEnum = z.enum(
  SUPPLIER_TYPES as [string, ...string[]],
) as unknown as z.ZodEnum<["Accommodation", "Activity", "Transport", "Restaurant", "Other"]>;

const serviceTypeEnum = z.enum(
  SERVICE_TYPES as [string, ...string[]],
) as unknown as z.ZodEnum<["Accommodation", "Activity", "Tour", "Transfer", "Meal", "Other"]>;

const pricingUnitEnum = z.enum(
  PRICING_UNITS as [string, ...string[]],
) as unknown as z.ZodEnum<
  ["PerPerson", "PerPersonPerNight", "PerRoomPerNight", "PerVehicle", "PerGroup"]
>;

const optionalNumber = z
  .union([z.number(), z.nan()])
  .optional()
  .transform((value) => (value == null || Number.isNaN(value) ? undefined : value));

export const serviceSchema = z.object({
  name: z.string().trim().min(1, "Service name is required").max(120),
  type: serviceTypeEnum,
  price: z
    .number({ invalid_type_error: "Price is required" })
    .min(0, "Price must be 0 or more"),
  currency: z
    .string()
    .trim()
    .regex(/^[A-Za-z]{3}$/, "Use a 3-letter code, e.g. ZAR")
    .transform((value) => value.toUpperCase()),
  pricingUnit: pricingUnitEnum,
  durationHours: optionalNumber.pipe(
    z.number().min(0, "Hours must be 0 or more").max(1000).optional(),
  ),
  durationMinutes: optionalNumber.pipe(
    z.number().min(0).max(59, "Minutes must be between 0 and 59").optional(),
  ),
  capacity: optionalNumber.pipe(
    z.number().min(1, "Capacity must be at least 1").optional(),
  ),
  description: z.string().max(2000).optional(),
});

export const supplierFormSchema = z.object({
  name: z.string().trim().min(1, "Supplier name is required").max(200),
  type: supplierTypeEnum,
  email: z.string().trim().min(1, "Email is required").email("Enter a valid email address"),
  phone: z.string().trim().max(40).optional(),
  website: z
    .string()
    .trim()
    .optional()
    .refine(
      (value) => !value || /^https?:\/\/[^\s.]+\.[^\s]{2,}$/i.test(value),
      "Enter a valid URL, starting with https://",
    ),
  addressLine: z.string().trim().max(250).optional(),
  city: z.string().trim().min(1, "City is required").max(120),
  country: z.string().trim().min(1, "Country is required").max(120),
  services: z
    .array(serviceSchema)
    .min(1, "Add at least one service")
    .max(50, "A supplier can have at most 50 services"),
});

export type SupplierFormValues = z.input<typeof supplierFormSchema>;
export type SupplierFormOutput = z.output<typeof supplierFormSchema>;

export const emptyService: SupplierFormValues["services"][number] = {
  name: "",
  type: "Accommodation",
  price: undefined as unknown as number,
  currency: "ZAR",
  pricingUnit: "PerPerson",
  durationHours: undefined,
  durationMinutes: undefined,
  capacity: undefined,
  description: "",
};

export const emptySupplierForm: SupplierFormValues = {
  name: "",
  type: "Accommodation",
  email: "",
  phone: "",
  website: "",
  addressLine: "",
  city: "",
  country: "South Africa",
  services: [{ ...emptyService }],
};

/** Maps validated form values onto the API's CreateSupplierRequest shape. */
export function toCreateRequest(values: SupplierFormOutput): CreateSupplierRequest {
  return {
    name: values.name,
    type: values.type,
    email: values.email,
    phone: values.phone || undefined,
    website: values.website || undefined,
    addressLine: values.addressLine || undefined,
    city: values.city,
    country: values.country,
    services: values.services.map((service) => {
      const hours = service.durationHours ?? 0;
      const minutes = service.durationMinutes ?? 0;
      const total = hours * 60 + minutes;
      return {
        name: service.name,
        description: service.description || undefined,
        type: service.type,
        price: service.price,
        currency: service.currency,
        pricingUnit: service.pricingUnit,
        durationMinutes: total > 0 ? total : undefined,
        capacity: service.capacity,
      };
    }),
  };
}

/** Converts an AI draft back into form values. */
export function draftToFormValues(draft: CreateSupplierRequest): SupplierFormValues {
  return {
    name: draft.name ?? "",
    type: draft.type ?? "Accommodation",
    email: draft.email ?? "",
    phone: draft.phone ?? "",
    website: draft.website ?? "",
    addressLine: draft.addressLine ?? "",
    city: draft.city ?? "",
    country: draft.country ?? "South Africa",
    services:
      draft.services?.length > 0
        ? draft.services.map((service) => ({
            name: service.name ?? "",
            type: service.type ?? "Activity",
            price: service.price ?? 0,
            currency: service.currency ?? "ZAR",
            pricingUnit: service.pricingUnit ?? "PerPerson",
            durationHours: service.durationMinutes
              ? Math.floor(service.durationMinutes / 60)
              : undefined,
            durationMinutes: service.durationMinutes
              ? service.durationMinutes % 60
              : undefined,
            capacity: service.capacity,
            description: service.description ?? "",
          }))
        : [{ ...emptyService }],
  };
}
