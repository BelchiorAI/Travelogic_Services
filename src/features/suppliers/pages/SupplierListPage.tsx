import { Link, getRouteApi } from "@tanstack/react-router";
import { AlertCircle, ChevronLeft, ChevronRight, Plus, PackageOpen } from "lucide-react";

import type { SupplierType } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useSuppliers } from "../api/useSuppliers";
import { SupplierCard } from "../components/SupplierCard";
import { SupplierFilters } from "../components/SupplierFilters";

const routeApi = getRouteApi("/");
const PAGE_SIZE = 12;

export function SupplierListPage() {
  const search = routeApi.useSearch();
  const navigate = routeApi.useNavigate();

  const query = useSuppliers({
    page: search.page,
    pageSize: PAGE_SIZE,
    search: search.search,
    type: search.type,
  });

  const setSearch = (value: string) =>
    navigate({ to: ".", search: (prev) => ({ ...prev, search: value || undefined, page: 1 }) });

  const setType = (value: SupplierType | "") =>
    navigate({ to: ".", search: (prev) => ({ ...prev, type: value || undefined, page: 1 }) });

  const setPage = (page: number) =>
    navigate({ to: ".", search: (prev) => ({ ...prev, page }) });

  const clearFilters = () =>
    navigate({ to: ".", search: { page: 1 } });

  const hasFilters = Boolean(search.search || search.type);
  const data = query.data;

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 sm:px-6 sm:py-10">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold sm:text-3xl">Supplier Hub</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Tourism suppliers and the services they sell.
          </p>
        </div>
        <Button asChild>
          <Link to="/suppliers/new">
            <Plus className="size-4" aria-hidden />
            Add supplier
          </Link>
        </Button>
      </header>

      <div className="mt-8">
        <SupplierFilters
          search={search.search ?? ""}
          type={search.type ?? ""}
          onSearchChange={setSearch}
          onTypeChange={setType}
        />
      </div>

      <div className="mt-6">
        {query.isPending && <SkeletonGrid />}

        {query.isError && (
          <div className="rounded-2xl border border-destructive/30 bg-destructive/5 p-8 text-center">
            <AlertCircle className="mx-auto size-6 text-destructive" aria-hidden />
            <h2 className="mt-3 text-lg font-semibold">We couldn't load the suppliers</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              The Supplier Hub API didn't respond as expected.
            </p>
            <Button className="mt-4" onClick={() => query.refetch()}>
              Try again
            </Button>
          </div>
        )}

        {data && data.items.length === 0 && (
          <div className="rounded-2xl border border-dashed border-border bg-card/50 p-10 text-center">
            <PackageOpen className="mx-auto size-7 text-muted-foreground" aria-hidden />
            {hasFilters ? (
              <>
                <h2 className="mt-3 text-lg font-semibold">No suppliers match your filters</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  Try a different search term or supplier type.
                </p>
                <Button variant="outline" className="mt-4" onClick={clearFilters}>
                  Clear filters
                </Button>
              </>
            ) : (
              <>
                <h2 className="mt-3 text-lg font-semibold">No suppliers yet</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  Capture your first hotel, lodge, operator or transfer company.
                </p>
                <Button asChild className="mt-4">
                  <Link to="/suppliers/new">
                    <Plus className="size-4" aria-hidden />
                    Add your first supplier
                  </Link>
                </Button>
              </>
            )}
          </div>
        )}

        {data && data.items.length > 0 && (
          <>
            <p className="text-sm text-muted-foreground">
              {data.totalCount} {data.totalCount === 1 ? "supplier" : "suppliers"}
            </p>
            <div className="mt-3 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {data.items.map((supplier) => (
                <SupplierCard key={supplier.id} supplier={supplier} />
              ))}
            </div>

            <nav
              aria-label="Pagination"
              className="mt-8 flex items-center justify-between gap-4"
            >
              <Button
                variant="outline"
                onClick={() => setPage(data.page - 1)}
                disabled={data.page <= 1}
              >
                <ChevronLeft className="size-4" aria-hidden />
                Previous
              </Button>
              <span className="text-sm text-muted-foreground">
                Page {data.page} of {data.totalPages}
              </span>
              <Button
                variant="outline"
                onClick={() => setPage(data.page + 1)}
                disabled={data.page >= data.totalPages}
              >
                Next
                <ChevronRight className="size-4" aria-hidden />
              </Button>
            </nav>
          </>
        )}
      </div>
    </div>
  );
}

function SkeletonGrid() {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 6 }).map((_, index) => (
        <div key={index} className="rounded-xl border border-border bg-card p-5">
          <Skeleton className="h-5 w-2/3" />
          <Skeleton className="mt-3 h-5 w-24 rounded-full" />
          <Skeleton className="mt-4 h-4 w-1/2" />
          <Skeleton className="mt-2 h-4 w-3/5" />
          <Skeleton className="mt-5 h-4 w-20" />
        </div>
      ))}
    </div>
  );
}
