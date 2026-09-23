import { Search, X } from "lucide-react";
import { useEffect, useState } from "react";

import { SUPPLIER_TYPES, type SupplierType } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const ALL = "__all__";

interface SupplierFiltersProps {
  search: string;
  type: SupplierType | "";
  onSearchChange: (value: string) => void;
  onTypeChange: (value: SupplierType | "") => void;
}

export function SupplierFilters({
  search,
  type,
  onSearchChange,
  onTypeChange,
}: SupplierFiltersProps) {
  const [draft, setDraft] = useState(search);

  // Keep the input in sync when the URL changes elsewhere (e.g. "Clear filters").
  useEffect(() => {
    setDraft(search);
  }, [search]);

  useEffect(() => {
    if (draft === search) return;
    const timer = setTimeout(() => onSearchChange(draft), 300);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draft]);

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
      <div className="flex-1">
        <Label htmlFor="supplier-search" className="mb-1.5 text-xs font-medium text-muted-foreground">
          Search suppliers
        </Label>
        <div className="relative">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden
          />
          <Input
            id="supplier-search"
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            placeholder="Name, city or email"
            className="bg-card pl-9 pr-9"
          />
          {draft && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="absolute right-1 top-1/2 size-7 -translate-y-1/2"
              onClick={() => setDraft("")}
            >
              <X className="size-4" />
              <span className="sr-only">Clear search</span>
            </Button>
          )}
        </div>
      </div>

      <div className="sm:w-56">
        <Label htmlFor="supplier-type" className="mb-1.5 text-xs font-medium text-muted-foreground">
          Supplier type
        </Label>
        <Select
          value={type === "" ? ALL : type}
          onValueChange={(value) => onTypeChange(value === ALL ? "" : (value as SupplierType))}
        >
          <SelectTrigger id="supplier-type" className="w-full bg-card">
            <SelectValue placeholder="All types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>All types</SelectItem>
            {SUPPLIER_TYPES.map((option) => (
              <SelectItem key={option} value={option}>
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </div>
  );
}
