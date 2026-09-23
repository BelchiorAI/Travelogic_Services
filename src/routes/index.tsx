import { createFileRoute } from "@tanstack/react-router";

import { SUPPLIER_TYPES, type SupplierType } from "@/api/types";
import { SupplierListPage } from "@/features/suppliers/pages/SupplierListPage";

interface SupplierSearch {
  page?: number | undefined;
  search?: string | undefined;
  type?: SupplierType | undefined;
}

export const Route = createFileRoute("/")({
  validateSearch: (search: Record<string, unknown>): SupplierSearch => {
    const page = Number(search["page"]);
    const type = String(search["type"] ?? "");
    const term = String(search["search"] ?? "").trim();
    return {
      page: Number.isFinite(page) && page > 0 ? Math.floor(page) : 1,
      search: term || undefined,
      type: SUPPLIER_TYPES.includes(type as SupplierType)
        ? (type as SupplierType)
        : undefined,
    };
  },
  head: () => ({
    meta: [
      { title: "Suppliers — Supplier Hub" },
      {
        name: "description",
        content:
          "Browse, search and filter the tourism suppliers and services captured in Supplier Hub.",
      },
      { property: "og:title", content: "Suppliers — Supplier Hub" },
      {
        property: "og:description",
        content:
          "Browse, search and filter the tourism suppliers and services captured in Supplier Hub.",
      },
    ],
  }),
  component: SupplierListPage,
});
