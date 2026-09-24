import { Link } from "@tanstack/react-router";
import { ChevronLeft } from "lucide-react";

import { SupplierForm } from "../components/SupplierForm";

export function CreateSupplierPage() {
  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 sm:py-10">
      <Link
        to="/"
        className="inline-flex items-center gap-1 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ChevronLeft className="size-4" aria-hidden />
        All suppliers
      </Link>

      <h1 className="mt-4 text-2xl font-semibold sm:text-3xl">Add supplier</h1>
      <p className="mb-6 mt-1 text-sm text-muted-foreground">
        Capture the supplier's contact details and every service they sell.
      </p>

      <SupplierForm />
    </div>
  );
}
