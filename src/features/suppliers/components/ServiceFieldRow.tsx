import { Trash2 } from "lucide-react";
import { Controller, useFormContext } from "react-hook-form";

import { PRICING_UNITS, SERVICE_TYPES } from "@/api/types";
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
import { Textarea } from "@/components/ui/textarea";
import { pricingUnitLabel } from "@/lib/formatters";
import type { SupplierFormValues } from "../schemas";

interface ServiceFieldRowProps {
  index: number;
  canRemove: boolean;
  onRemove: () => void;
}

export function ServiceFieldRow({ index, canRemove, onRemove }: ServiceFieldRowProps) {
  const { register, control, formState } = useFormContext<SupplierFormValues>();
  const errors = formState.errors.services?.[index];
  const id = (field: string) => `service-${index}-${field}`;

  return (
    <div className="rounded-xl border border-border bg-card p-4 shadow-[var(--shadow-card)] sm:p-5">
      <div className="mb-4 flex items-center justify-between">
        <p className="text-sm font-semibold text-muted-foreground">Service {index + 1}</p>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onRemove}
          disabled={!canRemove}
          className="text-muted-foreground hover:text-destructive"
        >
          <Trash2 className="size-4" aria-hidden />
          Remove
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <Label htmlFor={id("name")}>Service name *</Label>
          <Input id={id("name")} className="mt-1.5" {...register(`services.${index}.name`)} />
          <FieldError message={errors?.name?.message} />
        </div>

        <div>
          <Label htmlFor={id("type")}>Service type *</Label>
          <Controller
            control={control}
            name={`services.${index}.type`}
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id={id("type")} className="mt-1.5 w-full">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {SERVICE_TYPES.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors?.type?.message} />
        </div>

        <div>
          <Label htmlFor={id("pricingUnit")}>Pricing unit *</Label>
          <Controller
            control={control}
            name={`services.${index}.pricingUnit`}
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id={id("pricingUnit")} className="mt-1.5 w-full">
                  <SelectValue placeholder="Select unit" />
                </SelectTrigger>
                <SelectContent>
                  {PRICING_UNITS.map((option) => (
                    <SelectItem key={option} value={option}>
                      {pricingUnitLabel(option)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors?.pricingUnit?.message} />
        </div>

        <div>
          <Label htmlFor={id("price")}>Price *</Label>
          <Input
            id={id("price")}
            type="number"
            min={0}
            step="0.01"
            inputMode="decimal"
            className="mt-1.5"
            {...register(`services.${index}.price`, { valueAsNumber: true })}
          />
          <FieldError message={errors?.price?.message} />
        </div>

        <div>
          <Label htmlFor={id("currency")}>Currency</Label>
          <Input
            id={id("currency")}
            maxLength={3}
            className="mt-1.5 uppercase"
            {...register(`services.${index}.currency`)}
          />
          <FieldError message={errors?.currency?.message} />
        </div>

        <div>
          <fieldset>
            <legend className="text-sm font-medium">Duration</legend>
            <div className="mt-1.5 grid grid-cols-2 gap-2">
              <div>
                <Label htmlFor={id("hours")} className="sr-only">
                  Duration hours
                </Label>
                <Input
                  id={id("hours")}
                  type="number"
                  min={0}
                  placeholder="Hours"
                  {...register(`services.${index}.durationHours`, { valueAsNumber: true })}
                />
              </div>
              <div>
                <Label htmlFor={id("minutes")} className="sr-only">
                  Duration minutes
                </Label>
                <Input
                  id={id("minutes")}
                  type="number"
                  min={0}
                  max={59}
                  placeholder="Minutes"
                  {...register(`services.${index}.durationMinutes`, { valueAsNumber: true })}
                />
              </div>
            </div>
          </fieldset>
          <FieldError
            message={errors?.durationHours?.message ?? errors?.durationMinutes?.message}
          />
        </div>

        <div>
          <Label htmlFor={id("capacity")}>Capacity</Label>
          <Input
            id={id("capacity")}
            type="number"
            min={1}
            className="mt-1.5"
            {...register(`services.${index}.capacity`, { valueAsNumber: true })}
          />
          <FieldError message={errors?.capacity?.message} />
        </div>

        <div className="sm:col-span-2">
          <Label htmlFor={id("description")}>Description</Label>
          <Textarea
            id={id("description")}
            rows={3}
            className="mt-1.5"
            {...register(`services.${index}.description`)}
          />
          <FieldError message={errors?.description?.message} />
        </div>
      </div>
    </div>
  );
}

export function FieldError({ message }: { message?: string | undefined }) {
  if (!message) return null;
  return (
    <p role="alert" className="mt-1.5 text-sm font-medium text-destructive">
      {message}
    </p>
  );
}
