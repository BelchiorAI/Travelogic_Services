import { Link } from "@tanstack/react-router";
import { ArrowUpRight, Mail, MapPin } from "lucide-react";

import { mediaUrl } from "@/api/client";
import type { SupplierSummary } from "@/api/types";
import { Card } from "@/components/ui/card";
import { SupplierTypeBadge } from "./SupplierTypeBadge";

export function SupplierCard({ supplier }: { supplier: SupplierSummary }) {
  return (
    <Card className="group relative gap-0 overflow-hidden p-5 transition-colors hover:border-primary/40">
      {supplier.coverImageUrl && (
        <img
          src={mediaUrl(supplier.coverImageUrl)}
          alt=""
          loading="lazy"
          className="-mx-5 -mt-5 mb-4 aspect-[16/9] w-[calc(100%+2.5rem)] max-w-none object-cover"
        />
      )}
      <div className="flex items-start justify-between gap-3">
        <h3 className="text-base font-semibold leading-snug">
          <Link
            to="/suppliers/$id"
            params={{ id: supplier.id }}
            className="after:absolute after:inset-0 after:content-[''] focus-visible:outline-none"
          >
            {supplier.name}
          </Link>
        </h3>
        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:-translate-y-0.5 group-hover:text-primary" />
      </div>

      <div className="mt-3">
        <SupplierTypeBadge type={supplier.type} />
      </div>

      <dl className="mt-4 space-y-1.5 text-sm text-muted-foreground">
        <div className="flex items-center gap-2">
          <MapPin className="size-3.5 shrink-0" aria-hidden />
          <dd>
            {supplier.city}, {supplier.country}
          </dd>
        </div>
        <div className="flex items-center gap-2">
          <Mail className="size-3.5 shrink-0" aria-hidden />
          <dd className="truncate">{supplier.email}</dd>
        </div>
      </dl>

      <p className="mt-4 border-t border-border pt-3 text-sm font-medium text-foreground">
        {supplier.serviceCount} {supplier.serviceCount === 1 ? "service" : "services"}
      </p>
    </Card>
  );
}
