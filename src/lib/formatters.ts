import type { PricingUnit, ServiceType, SupplierType } from "@/api/types";

/** Formats an amount as South African Rand, e.g. "R 4 500,00". */
export function formatCurrency(amount: number, currency = "ZAR"): string {
  const negative = amount < 0;
  const value = Math.abs(amount);
  const whole = Math.floor(value);
  const cents = Math.round((value - whole) * 100);

  // Space-grouped thousands with a comma decimal separator (South African style).
  const grouped = whole.toString().replace(/\B(?=(\d{3})+(?!\d))/g, " ");
  const symbol = currency === "ZAR" ? "R" : currency;

  return `${negative ? "-" : ""}${symbol} ${grouped},${cents.toString().padStart(2, "0")}`;
}

/** 245 -> "4 h 5 min", 240 -> "4 h", 45 -> "45 min" */
export function formatDuration(minutes: number | null | undefined): string {
  if (minutes == null) return "—";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h && m) return `${h} h ${m} min`;
  if (h) return `${h} h`;
  return `${m} min`;
}

const PRICING_UNIT_LABELS: Record<PricingUnit, string> = {
  PerPerson: "per person",
  PerPersonPerNight: "per person per night",
  PerRoomPerNight: "per room per night",
  PerVehicle: "per vehicle",
  PerGroup: "per group",
};

export function pricingUnitLabel(unit: PricingUnit): string {
  return PRICING_UNIT_LABELS[unit] ?? enumLabel(unit);
}

/** "PerRoomPerNight" -> "per room per night" (generic fallback) */
export function enumLabel(value: string): string {
  return value
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .toLowerCase()
    .trim();
}

export function supplierTypeLabel(type: SupplierType): string {
  return type;
}

export function serviceTypeLabel(type: ServiceType): string {
  return type;
}

/** "2026-09-23T10:00:00Z" -> "23 Sep 2026" */
export function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "—";
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

export function minutesToHm(minutes: number | null | undefined) {
  if (minutes == null) return { hours: "", minutes: "" };
  return {
    hours: String(Math.floor(minutes / 60)),
    minutes: String(minutes % 60),
  };
}
