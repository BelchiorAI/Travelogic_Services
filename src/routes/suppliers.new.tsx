import { createFileRoute } from "@tanstack/react-router";

import { CreateSupplierPage } from "@/features/suppliers/pages/CreateSupplierPage";

export const Route = createFileRoute("/suppliers/new")({
  head: () => ({
    meta: [
      { title: "Add supplier — Supplier Hub" },
      {
        name: "description",
        content:
          "Capture a new tourism supplier and all of its services, or import the details from a rate sheet with AI.",
      },
      { property: "og:title", content: "Add supplier — Supplier Hub" },
      {
        property: "og:description",
        content:
          "Capture a new tourism supplier and all of its services, or import the details from a rate sheet with AI.",
      },
    ],
  }),
  component: CreateSupplierPage,
});
