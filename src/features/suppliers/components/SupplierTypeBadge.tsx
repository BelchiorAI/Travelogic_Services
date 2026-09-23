import type { ServiceType, SupplierType } from "@/api/types";
import { cn } from "@/lib/utils";

const SUPPLIER_STYLES: Record<SupplierType, string> = {
  Accommodation: "bg-type-accommodation text-type-accommodation-foreground",
  Activity: "bg-type-activity text-type-activity-foreground",
  Transport: "bg-type-transport text-type-transport-foreground",
  Restaurant: "bg-type-restaurant text-type-restaurant-foreground",
  Other: "bg-type-other text-type-other-foreground",
};

const SERVICE_STYLES: Record<ServiceType, string> = {
  Accommodation: "bg-type-accommodation text-type-accommodation-foreground",
  Activity: "bg-type-activity text-type-activity-foreground",
  Tour: "bg-type-activity text-type-activity-foreground",
  Transfer: "bg-type-transport text-type-transport-foreground",
  Meal: "bg-type-restaurant text-type-restaurant-foreground",
  Other: "bg-type-other text-type-other-foreground",
};

const base =
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium tracking-wide";

export function SupplierTypeBadge({
  type,
  className,
}: {
  type: SupplierType;
  className?: string;
}) {
  return <span className={cn(base, SUPPLIER_STYLES[type], className)}>{type}</span>;
}

export function ServiceTypeBadge({
  type,
  className,
}: {
  type: ServiceType;
  className?: string;
}) {
  return <span className={cn(base, SERVICE_STYLES[type], className)}>{type}</span>;
}
