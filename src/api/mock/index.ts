import { ApiError } from "../errors";
import type {
  CreateSupplierRequest,
  ExtractSupplierResponse,
  FeatureFlags,
  PagedResult,
  Supplier,
  SupplierListParams,
  SupplierSummary,
} from "../types";
import { mockSuppliers } from "./data";

/** In-memory store so newly created suppliers persist for the session. */
const store: Supplier[] = mockSuppliers.map((s) => ({ ...s }));

const wait = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
const latency = () => wait(300 + Math.random() * 400);

function toSummary(supplier: Supplier): SupplierSummary {
  return {
    id: supplier.id,
    name: supplier.name,
    type: supplier.type,
    city: supplier.city,
    country: supplier.country,
    email: supplier.email,
    serviceCount: supplier.services.length,
    createdAt: supplier.createdAt,
  };
}

export async function mockGetSuppliers(
  params: SupplierListParams,
): Promise<PagedResult<SupplierSummary>> {
  await latency();
  const page = params.page && params.page > 0 ? params.page : 1;
  const pageSize = params.pageSize && params.pageSize > 0 ? params.pageSize : 12;
  const search = (params.search ?? "").trim().toLowerCase();

  let items = [...store].sort((a, b) => a.name.localeCompare(b.name));

  if (search) {
    items = items.filter((s) =>
      [s.name, s.city, s.country, s.email].some((field) =>
        field.toLowerCase().includes(search),
      ),
    );
  }
  if (params.type) {
    items = items.filter((s) => s.type === params.type);
  }

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;

  return {
    items: items.slice(start, start + pageSize).map(toSummary),
    page,
    pageSize,
    totalCount,
    totalPages,
  };
}

export async function mockGetSupplier(id: string): Promise<Supplier> {
  await latency();
  const supplier = store.find((s) => s.id === id);
  if (!supplier) {
    throw new ApiError(404, "Not Found", {
      title: "Not Found",
      status: 404,
      detail: `No supplier exists with id '${id}'.`,
    });
  }
  return structuredClone(supplier);
}

export async function mockCreateSupplier(
  body: CreateSupplierRequest,
): Promise<Supplier> {
  await latency();

  // Example 400 ProblemDetails response, mirroring the real API.
  const errors: Record<string, string[]> = {};
  if (body.email.toLowerCase().endsWith("@example.com")) {
    errors["Email"] = ["Example domains are not accepted for suppliers."];
  }
  if (body.name.trim().toLowerCase() === "test") {
    errors["Name"] = ["'Test' is not a valid supplier name."];
  }
  body.services.forEach((service, index) => {
    if (service.price > 1_000_000) {
      errors[`Services[${index}].Price`] = [
        "Price may not exceed R 1 000 000,00.",
      ];
    }
    if (service.currency.toUpperCase() !== service.currency) {
      errors[`Services[${index}].Currency`] = [
        "Currency must be an uppercase 3-letter code.",
      ];
    }
  });

  if (Object.keys(errors).length > 0) {
    throw new ApiError(400, "One or more validation errors occurred.", {
      title: "One or more validation errors occurred.",
      status: 400,
      errors,
    });
  }

  const now = new Date().toISOString();
  const supplier: Supplier = {
    id: `sup-${Math.floor(Math.random() * 90000 + 10000)}`,
    name: body.name,
    type: body.type,
    email: body.email,
    phone: body.phone || null,
    website: body.website || null,
    addressLine: body.addressLine || null,
    city: body.city,
    country: body.country,
    createdAt: now,
    updatedAt: now,
    services: body.services.map((service, index) => ({
      id: `svc-new-${Date.now()}-${index}`,
      name: service.name,
      description: service.description || null,
      type: service.type,
      price: service.price,
      currency: service.currency,
      pricingUnit: service.pricingUnit,
      durationMinutes: service.durationMinutes ?? null,
      capacity: service.capacity ?? null,
      isActive: true,
    })),
  };

  store.unshift(supplier);
  return structuredClone(supplier);
}

export async function mockGetFeatures(): Promise<FeatureFlags> {
  await wait(150);
  return { aiExtraction: true };
}

export async function mockExtractSupplier(
  text: string,
): Promise<ExtractSupplierResponse> {
  await wait(1200);
  if (!text.trim()) {
    throw new ApiError(400, "One or more validation errors occurred.", {
      title: "One or more validation errors occurred.",
      status: 400,
      errors: { Text: ["Paste some text before extracting."] },
    });
  }

  return {
    draft: {
      name: "Nkosi Bush Lodge",
      type: "Accommodation",
      email: "reservations@nkosibushlodge.co.za",
      phone: "+27 13 590 2233",
      website: "https://nkosibushlodge.co.za",
      addressLine: "Orpen Gate Road, Kruger National Park",
      city: "Hoedspruit",
      country: "South Africa",
      services: [
        {
          name: "Luxury Chalet (full board)",
          description:
            "Thatched chalet sleeping two, includes all meals and two daily game activities.",
          type: "Accommodation",
          price: 5850,
          currency: "ZAR",
          pricingUnit: "PerPersonPerNight",
          capacity: 2,
        },
        {
          name: "Sunset Game Drive",
          description: "Three-hour open-vehicle drive with sundowners.",
          type: "Activity",
          price: 1150,
          currency: "ZAR",
          pricingUnit: "PerPerson",
          durationMinutes: 180,
          capacity: 9,
        },
        {
          name: "Guided Bush Walk",
          description: "Morning walk with two armed field guides.",
          type: "Activity",
          price: 890,
          currency: "ZAR",
          pricingUnit: "PerPerson",
          durationMinutes: 150,
          capacity: 8,
        },
      ],
    },
    warnings: [
      "Rates in the source text were listed for the 2026 green season only — confirm the validity period before saving.",
    ],
  };
}
