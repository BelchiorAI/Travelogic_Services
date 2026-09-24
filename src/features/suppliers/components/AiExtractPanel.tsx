import { AlertTriangle, ChevronDown, Loader2, Sparkles } from "lucide-react";
import { useState } from "react";

import { ApiError } from "@/api/client";
import type { ExtractionWarning } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import { useExtractSupplier } from "../api/useExtractSupplier";
import { useFeatures } from "../api/useFeatures";
import { draftToFormValues, type SupplierFormValues } from "../schemas";

const MAX_CHARS = 20000;

interface AiExtractPanelProps {
  hasData: boolean;
  /** Warnings with a field key are also passed on so the form can highlight those fields. */
  onApplyDraft: (values: SupplierFormValues, warnings: ExtractionWarning[]) => void;
}

export function AiExtractPanel({ hasData, onApplyDraft }: AiExtractPanelProps) {
  const { data: features } = useFeatures();
  const extract = useExtractSupplier();
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [warnings, setWarnings] = useState<ExtractionWarning[]>([]);
  const [error, setError] = useState<string | null>(null);

  if (!features) return null;

  // Stay visible when the server has no AI model configured, so people know the feature exists.
  if (!features.aiExtraction) {
    return (
      <section className="flex items-center gap-3 rounded-2xl border border-dashed border-border bg-card p-5">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <Sparkles className="size-4" aria-hidden />
        </span>
        <span>
          <span className="block font-semibold">Import with AI</span>
          <span className="block text-sm text-muted-foreground">
            Not switched on for this server yet. Once an AI key is added to the Supplier API, you
            can paste a rate sheet or email here and have the form filled in for you.
          </span>
        </span>
      </section>
    );
  }

  const handleExtract = async () => {
    setError(null);
    if (hasData && !window.confirm("This will replace what you've already filled in. Continue?")) {
      return;
    }
    try {
      const result = await extract.mutateAsync(text);
      onApplyDraft(draftToFormValues(result.draft), result.warnings);
      setWarnings(result.warnings);
    } catch (err) {
      setWarnings([]);
      setError(
        err instanceof ApiError
          ? (err.detail ?? err.title)
          : "We couldn't read that text. Please try again.",
      );
    }
  };

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-[var(--shadow-card)]">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        className="flex w-full items-center gap-3 p-5 text-left"
      >
        <span className="flex size-9 items-center justify-center rounded-full bg-accent text-accent-foreground">
          <Sparkles className="size-4" aria-hidden />
        </span>
        <span className="flex-1">
          <span className="block font-semibold">Import with AI</span>
          <span className="block text-sm text-muted-foreground">
            Paste a rate sheet, contract excerpt or email and we'll fill in the form for you.
            Review everything before saving.
          </span>
        </span>
        <ChevronDown
          className={cn("size-5 shrink-0 text-muted-foreground transition-transform", open && "rotate-180")}
          aria-hidden
        />
      </button>

      {open && (
        <div className="border-t border-border p-5">
          <Label htmlFor="ai-text">Source text</Label>
          <Textarea
            id="ai-text"
            rows={7}
            value={text}
            maxLength={MAX_CHARS}
            onChange={(event) => setText(event.target.value)}
            placeholder="Paste the supplier's rate sheet or email here…"
            className="mt-1.5"
          />
          <div className="mt-2 flex items-center justify-between gap-4">
            <span className="text-xs text-muted-foreground">
              {text.length.toLocaleString("en-ZA")} / {MAX_CHARS.toLocaleString("en-ZA")} characters
            </span>
            <Button
              type="button"
              onClick={handleExtract}
              disabled={extract.isPending || text.trim().length === 0}
            >
              {extract.isPending && <Loader2 className="size-4 animate-spin" aria-hidden />}
              Extract
            </Button>
          </div>

          {error && (
            <p role="alert" className="mt-3 text-sm font-medium text-destructive">
              {error}
            </p>
          )}

          {warnings.length > 0 && (
            <div
              role="status"
              className="mt-4 flex items-start gap-3 rounded-xl border border-warning/40 bg-warning-surface p-4 text-sm text-warning-foreground"
            >
              <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
              <ul className="space-y-1">
                {warnings.map((warning, index) => (
                  <li key={`${warning.field}-${index}`}>{warning.message}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
