import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "@tanstack/react-router";
import { AlertCircle, Loader2, Plus, Sparkles } from "lucide-react";
import { useState } from "react";
import { FormProvider, useFieldArray, useForm, type UseFormReturn } from "react-hook-form";
import { toast } from "sonner";

import { ApiError } from "@/api/client";
import { SUPPLIER_TYPES } from "@/api/types";
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
import { useCreateSupplier } from "../api/useCreateSupplier";
import {
  emptyService,
  emptySupplierForm,
  supplierFormSchema,
  toCreateRequest,
  type SupplierFormOutput,
  type SupplierFormValues,
} from "../schemas";
import { AiExtractPanel } from "./AiExtractPanel";
import { FieldError, ServiceFieldRow } from "./ServiceFieldRow";

/** "Services[0].Price" -> "services.0.price" */
export function problemKeyToFieldPath(key: string): string {
  return key
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((part) =>
      /^\d+$/.test(part) ? part : part.charAt(0).toLowerCase() + part.slice(1),
    )
    .join(".");
}

export function SupplierForm() {
  const navigate = useNavigate();
  const createSupplier = useCreateSupplier();
  const [formError, setFormError] = useState<string | null>(null);
  const [aiFilled, setAiFilled] = useState(false);

  const form = useForm<SupplierFormValues, unknown, SupplierFormOutput>({
    resolver: zodResolver(supplierFormSchema),
    defaultValues: emptySupplierForm,
    mode: "onBlur",
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "services",
  });

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null);
    try {
      const supplier = await createSupplier.mutateAsync(toCreateRequest(values));
      toast.success("Supplier saved", {
        description: `${supplier.name} was added with ${supplier.services.length} service(s).`,
      });
      navigate({ to: "/suppliers/$id", params: { id: supplier.id } });
    } catch (error) {
      handleApiError(error, form, setFormError);
    }
  });

  const handleCancel = () => {
    if (
      form.formState.isDirty &&
      !window.confirm("You have unsaved changes. Leave without saving?")
    ) {
      return;
    }
    navigate({ to: "/" });
  };

  const servicesError = form.formState.errors.services?.message;

  return (
    <FormProvider {...form}>
      <AiExtractPanel
        hasData={form.formState.isDirty}
        onApplyDraft={(values) => {
          form.reset(values);
          setAiFilled(true);
        }}
      />

      <form onSubmit={onSubmit} noValidate className="mt-6 pb-28">
        {aiFilled && (
          <p className="mb-4 inline-flex items-center gap-2 rounded-full bg-accent px-3 py-1 text-xs font-medium text-accent-foreground">
            <Sparkles className="size-3.5" aria-hidden />
            Filled by AI — please review
          </p>
        )}

        {formError && (
          <div
            role="alert"
            className="mb-6 flex items-start gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive"
          >
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <p>{formError}</p>
          </div>
        )}

        <section className="rounded-2xl border border-border bg-card p-5 shadow-[var(--shadow-card)] sm:p-6">
          <h2 className="text-lg font-semibold">Supplier details</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Who the supplier is and how to reach them.
          </p>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="name">Supplier name *</Label>
              <Input id="name" className="mt-1.5" {...form.register("name")} />
              <FieldError message={form.formState.errors.name?.message} />
            </div>

            <div>
              <Label htmlFor="type">Supplier type *</Label>
              <Select
                value={form.watch("type")}
                onValueChange={(value) =>
                  form.setValue("type", value as SupplierFormValues["type"], {
                    shouldDirty: true,
                  })
                }
              >
                <SelectTrigger id="type" className="mt-1.5 w-full">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {SUPPLIER_TYPES.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FieldError message={form.formState.errors.type?.message} />
            </div>

            <div>
              <Label htmlFor="email">Email *</Label>
              <Input id="email" type="email" className="mt-1.5" {...form.register("email")} />
              <FieldError message={form.formState.errors.email?.message} />
            </div>

            <div>
              <Label htmlFor="phone">Phone</Label>
              <Input id="phone" className="mt-1.5" {...form.register("phone")} />
              <FieldError message={form.formState.errors.phone?.message} />
            </div>

            <div>
              <Label htmlFor="website">Website</Label>
              <Input
                id="website"
                placeholder="https://"
                className="mt-1.5"
                {...form.register("website")}
              />
              <FieldError message={form.formState.errors.website?.message} />
            </div>

            <div className="sm:col-span-2">
              <Label htmlFor="addressLine">Address line</Label>
              <Input id="addressLine" className="mt-1.5" {...form.register("addressLine")} />
              <FieldError message={form.formState.errors.addressLine?.message} />
            </div>

            <div>
              <Label htmlFor="city">City *</Label>
              <Input id="city" className="mt-1.5" {...form.register("city")} />
              <FieldError message={form.formState.errors.city?.message} />
            </div>

            <div>
              <Label htmlFor="country">Country *</Label>
              <Input id="country" className="mt-1.5" {...form.register("country")} />
              <FieldError message={form.formState.errors.country?.message} />
            </div>
          </div>
        </section>

        <section className="mt-6">
          <div className="flex items-end justify-between gap-4">
            <div>
              <h2 className="text-lg font-semibold">Services</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Everything this supplier sells — rooms, drives, transfers or meals.
              </p>
            </div>
            <span className="text-sm text-muted-foreground">{fields.length} of 50</span>
          </div>

          {typeof servicesError === "string" && <FieldError message={servicesError} />}

          <div className="mt-4 space-y-4">
            {fields.map((field, index) => (
              <ServiceFieldRow
                key={field.id}
                index={index}
                canRemove={fields.length > 1}
                onRemove={() => remove(index)}
              />
            ))}
          </div>

          <Button
            type="button"
            variant="outline"
            className="mt-4"
            disabled={fields.length >= 50}
            onClick={() => append({ ...emptyService })}
          >
            <Plus className="size-4" aria-hidden />
            Add another service
          </Button>
        </section>

        <div className="fixed inset-x-0 bottom-0 z-10 border-t border-border bg-background/95 backdrop-blur">
          <div className="mx-auto flex max-w-4xl items-center justify-end gap-3 px-4 py-3 sm:px-6">
            <Button type="button" variant="ghost" onClick={handleCancel}>
              Cancel
            </Button>
            <Button type="submit" disabled={form.formState.isSubmitting}>
              {form.formState.isSubmitting && (
                <Loader2 className="size-4 animate-spin" aria-hidden />
              )}
              Save supplier
            </Button>
          </div>
        </div>
      </form>
    </FormProvider>
  );
}

function handleApiError(
  error: unknown,
  form: UseFormReturn<SupplierFormValues, unknown, SupplierFormOutput>,
  setFormError: (message: string | null) => void,
) {
  if (error instanceof ApiError && error.errors) {
    const generic: string[] = [];
    for (const [key, messages] of Object.entries(error.errors)) {
      const path = problemKeyToFieldPath(key);
      const message = messages.join(" ");
      if (form.getFieldState(path as never)) {
        form.setError(path as never, { type: "server", message });
      } else {
        generic.push(message);
      }
    }
    setFormError(
      generic.length > 0 ? generic.join(" ") : "Please fix the highlighted fields and try again.",
    );
    toast.error("Supplier could not be saved");
    return;
  }

  const message =
    error instanceof ApiError
      ? (error.detail ?? error.title)
      : "Something went wrong while saving. Please try again.";
  setFormError(message);
  toast.error("Supplier could not be saved", { description: message });
}
