import { createFileRoute } from "@tanstack/react-router";

import { SupplierDetailPage } from "@/features/suppliers/pages/SupplierDetailPage";

export const Route = createFileRoute("/suppliers/$id")({
  head: () => ({
    meta: [
      { title: "Supplier details — Supplier Hub" },
      {
        name: "description",
        content:
          "Contact details, address and the full service list for a tourism supplier.",
      },
      { property: "og:title", content: "Supplier details — Supplier Hub" },
      {
        property: "og:description",
        content:
          "Contact details, address and the full service list for a tourism supplier.",
      },
    ],
  }),
  component: SupplierDetailPage,
});
