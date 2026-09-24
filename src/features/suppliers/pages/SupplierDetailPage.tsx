import { Link, getRouteApi } from "@tanstack/react-router";
import { ChevronLeft, Globe, Mail, MapPin, Phone } from "lucide-react";
import type { ReactNode } from "react";

import { ApiError } from "@/api/client";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { formatDate } from "@/lib/formatters";
import { useSupplier } from "../api/useSupplier";
import { ServiceList } from "../components/ServiceList";
import { SupplierMediaGallery } from "../components/SupplierMediaGallery";
import { SupplierTypeBadge } from "../components/SupplierTypeBadge";

const routeApi = getRouteApi("/suppliers/$id");

export function SupplierDetailPage() {
  const { id } = routeApi.useParams();
  const query = useSupplier(id);

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 sm:px-6 sm:py-10">
      <Link
        to="/"
        className="inline-flex items-center gap-1 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ChevronLeft className="size-4" aria-hidden />
        All suppliers
      </Link>

      {query.isPending && (
        <div className="mt-6 space-y-4">
          <Skeleton className="h-9 w-2/3 max-w-md" />
          <Skeleton className="h-6 w-32 rounded-full" />
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      )}

      {query.isError && (
        <div className="mt-10 rounded-2xl border border-border bg-card p-10 text-center">
          <h1 className="text-xl font-semibold">
            {query.error instanceof ApiError && query.error.isNotFound
              ? "Supplier not found"
              : "We couldn't load this supplier"}
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">
            {query.error instanceof ApiError && query.error.isNotFound
              ? "It may have been removed, or the link is out of date."
              : "The Supplier Hub API didn't respond as expected."}
          </p>
          <div className="mt-5 flex flex-wrap justify-center gap-3">
            <Button asChild variant="outline">
              <Link to="/">Back to suppliers</Link>
            </Button>
            {!(query.error instanceof ApiError && query.error.isNotFound) && (
              <Button onClick={() => query.refetch()}>Try again</Button>
            )}
          </div>
        </div>
      )}

      {query.data && (
        <article className="mt-6">
          <header className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 className="text-2xl font-semibold sm:text-3xl">{query.data.name}</h1>
              <div className="mt-2 flex flex-wrap items-center gap-3">
                <SupplierTypeBadge type={query.data.type} />
                <span className="text-sm text-muted-foreground">
                  Added {formatDate(query.data.createdAt)}
                </span>
              </div>
            </div>
          </header>

          <section className="mt-6 rounded-2xl border border-border bg-card p-5 shadow-[var(--shadow-card)] sm:p-6">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Contact
            </h2>
            <dl className="mt-4 grid gap-4 sm:grid-cols-2">
              <Detail icon={<Mail className="size-4" aria-hidden />} label="Email">
                {query.data.email ? (
                  <a className="hover:underline" href={`mailto:${query.data.email}`}>
                    {query.data.email}
                  </a>
                ) : (
                  "—"
                )}
              </Detail>
              <Detail icon={<Phone className="size-4" aria-hidden />} label="Phone">
                {query.data.phone ?? "—"}
              </Detail>
              <Detail icon={<Globe className="size-4" aria-hidden />} label="Website">
                {query.data.website ? (
                  <a
                    className="hover:underline"
                    href={query.data.website}
                    target="_blank"
                    rel="noreferrer"
                  >
                    {query.data.website}
                  </a>
                ) : (
                  "—"
                )}
              </Detail>
              <Detail icon={<MapPin className="size-4" aria-hidden />} label="Address">
                {[query.data.addressLine, query.data.city, query.data.country]
                  .filter(Boolean)
                  .join(", ")}
              </Detail>
            </dl>
          </section>

          <SupplierMediaGallery supplierId={query.data.id} media={query.data.media} />

          <section className="mt-8">
            <h2 className="mb-4 text-lg font-semibold">
              Services ({query.data.services.length})
            </h2>
            <ServiceList services={query.data.services} />
          </section>
        </article>
      )}
    </div>
  );
}

function Detail({
  icon,
  label,
  children,
}: {
  icon: ReactNode;
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="flex items-start gap-3">
      <span className="mt-0.5 text-muted-foreground">{icon}</span>
      <div>
        <dt className="text-xs text-muted-foreground">{label}</dt>
        <dd className="text-sm">{children}</dd>
      </div>
    </div>
  );
}
