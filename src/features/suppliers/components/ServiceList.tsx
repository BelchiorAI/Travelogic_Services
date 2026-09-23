import type { Service } from "@/api/types";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatCurrency, formatDuration, pricingUnitLabel } from "@/lib/formatters";
import { ServiceTypeBadge } from "./SupplierTypeBadge";

function StatusPill({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={
        isActive
          ? "inline-flex items-center gap-1.5 rounded-full bg-success/10 px-2.5 py-0.5 text-xs font-medium text-success"
          : "inline-flex items-center gap-1.5 rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground"
      }
    >
      <span
        aria-hidden
        className={`size-1.5 rounded-full ${isActive ? "bg-success" : "bg-muted-foreground"}`}
      />
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}

export function ServiceList({ services }: { services: Service[] }) {
  if (services.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-border p-6 text-sm text-muted-foreground">
        This supplier has no services captured yet.
      </p>
    );
  }

  return (
    <>
      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-2xl border border-border bg-card md:block">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Service</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Price</TableHead>
              <TableHead>Duration</TableHead>
              <TableHead>Capacity</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {services.map((service) => (
              <TableRow key={service.id}>
                <TableCell className="max-w-xs align-top">
                  <span className="font-medium">{service.name}</span>
                  {service.description && (
                    <span className="mt-0.5 block text-xs text-muted-foreground">
                      {service.description}
                    </span>
                  )}
                </TableCell>
                <TableCell className="align-top">
                  <ServiceTypeBadge type={service.type} />
                </TableCell>
                <TableCell className="align-top whitespace-nowrap">
                  <span className="font-medium">
                    {formatCurrency(service.price, service.currency)}
                  </span>
                  <span className="block text-xs text-muted-foreground">
                    {pricingUnitLabel(service.pricingUnit)}
                  </span>
                </TableCell>
                <TableCell className="align-top">{formatDuration(service.durationMinutes)}</TableCell>
                <TableCell className="align-top">{service.capacity ?? "—"}</TableCell>
                <TableCell className="align-top">
                  <StatusPill isActive={service.isActive} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Mobile cards */}
      <ul className="space-y-3 md:hidden">
        {services.map((service) => (
          <li
            key={service.id}
            className="rounded-xl border border-border bg-card p-4 shadow-[var(--shadow-card)]"
          >
            <div className="flex items-start justify-between gap-3">
              <p className="font-medium">{service.name}</p>
              <StatusPill isActive={service.isActive} />
            </div>
            <div className="mt-2">
              <ServiceTypeBadge type={service.type} />
            </div>
            {service.description && (
              <p className="mt-2 text-sm text-muted-foreground">{service.description}</p>
            )}
            <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
              <div>
                <dt className="text-xs text-muted-foreground">Price</dt>
                <dd className="font-medium">{formatCurrency(service.price, service.currency)}</dd>
                <dd className="text-xs text-muted-foreground">
                  {pricingUnitLabel(service.pricingUnit)}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Duration</dt>
                <dd>{formatDuration(service.durationMinutes)}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Capacity</dt>
                <dd>{service.capacity ?? "—"}</dd>
              </div>
            </dl>
          </li>
        ))}
      </ul>
    </>
  );
}
