import { ImagePlus, Loader2, Play, Trash2 } from "lucide-react";
import { useRef, useState, type ChangeEvent } from "react";
import { toast } from "sonner";

import { ApiError, mediaUrl } from "@/api/client";
import { MEDIA_RULES, type Media } from "@/api/types";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { useDeleteSupplierMedia, useUploadSupplierMedia } from "../api/useSupplierMedia";

interface SupplierMediaGalleryProps {
  supplierId: string;
  media: Media[];
}

const MB = 1024 * 1024;

/** Checks the same rules as the API before uploading, so a wrong file fails instantly. */
function preCheck(file: File): string | null {
  if (!MEDIA_RULES.accept.split(",").includes(file.type)) {
    return "Only JPEG, PNG or WebP photos and MP4 or WebM videos are supported.";
  }
  const isVideo = file.type.startsWith("video/");
  const max = isVideo ? MEDIA_RULES.maxVideoBytes : MEDIA_RULES.maxImageBytes;
  if (file.size > max) {
    return `${isVideo ? "Videos" : "Photos"} can be at most ${max / MB} MB (this one is ${(file.size / MB).toFixed(1)} MB).`;
  }
  return null;
}

function uploadErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.errors?.["File"]?.join(" ") ?? error.detail ?? error.title;
  }
  return "Upload failed. Please try again.";
}

export function SupplierMediaGallery({ supplierId, media }: SupplierMediaGalleryProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const upload = useUploadSupplierMedia(supplierId);
  const remove = useDeleteSupplierMedia(supplierId);
  const [progress, setProgress] = useState<{ current: number; total: number; fraction: number } | null>(
    null,
  );
  const [errors, setErrors] = useState<string[]>([]);
  const [viewing, setViewing] = useState<Media | null>(null);
  const [deleting, setDeleting] = useState<Media | null>(null);

  const remaining = MEDIA_RULES.maxPerSupplier - media.length;

  const handleFiles = async (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    event.target.value = "";
    if (files.length === 0) return;

    const problems: string[] = [];
    const accepted = files.filter((file) => {
      const problem = preCheck(file);
      if (problem) problems.push(`${file.name}: ${problem}`);
      return !problem;
    });
    if (accepted.length > remaining) {
      problems.push(
        `Only ${remaining} more photo${remaining === 1 ? "" : "s"} or video${remaining === 1 ? "" : "s"} fit (the limit is ${MEDIA_RULES.maxPerSupplier}).`,
      );
      accepted.splice(remaining);
    }

    // One at a time: large videos in parallel would compete for bandwidth and all finish late.
    let uploaded = 0;
    for (const [index, file] of accepted.entries()) {
      setProgress({ current: index + 1, total: accepted.length, fraction: 0 });
      try {
        await upload.mutateAsync({
          file,
          onProgress: (fraction) =>
            setProgress({ current: index + 1, total: accepted.length, fraction }),
        });
        uploaded++;
      } catch (error) {
        problems.push(`${file.name}: ${uploadErrorMessage(error)}`);
      }
    }
    setProgress(null);
    setErrors(problems);
    if (uploaded > 0) toast.success(`${uploaded} file${uploaded === 1 ? "" : "s"} uploaded`);
  };

  const confirmDelete = async () => {
    if (!deleting) return;
    const target = deleting;
    setDeleting(null);
    try {
      await remove.mutateAsync(target.id);
      toast.success(`${target.fileName} removed`);
    } catch (error) {
      toast.error("Could not remove the file", { description: uploadErrorMessage(error) });
    }
  };

  return (
    <section className="mt-8">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold">Photos &amp; videos ({media.length})</h2>
          <p className="text-sm text-muted-foreground">
            JPEG, PNG or WebP photos up to 10 MB · MP4 or WebM videos up to 100 MB · up to{" "}
            {MEDIA_RULES.maxPerSupplier} files
          </p>
        </div>
        <input
          ref={inputRef}
          type="file"
          accept={MEDIA_RULES.accept}
          multiple
          className="sr-only"
          aria-label="Choose photos or videos to upload"
          onChange={handleFiles}
        />
        <Button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={progress !== null || remaining <= 0}
        >
          {progress ? (
            <>
              <Loader2 className="size-4 animate-spin" aria-hidden />
              Uploading {progress.total > 1 ? `${progress.current} of ${progress.total} · ` : ""}
              {Math.round(progress.fraction * 100)}%
            </>
          ) : (
            <>
              <ImagePlus className="size-4" aria-hidden />
              Upload
            </>
          )}
        </Button>
      </div>

      {errors.length > 0 && (
        <ul
          role="alert"
          className="mb-4 space-y-1 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm text-destructive"
        >
          {errors.map((error, index) => (
            <li key={index}>{error}</li>
          ))}
        </ul>
      )}

      {media.length === 0 ? (
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          className="flex w-full flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-border bg-card p-10 text-sm text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
        >
          <ImagePlus className="size-6" aria-hidden />
          No photos or videos yet. Upload some to show guests what to expect.
        </button>
      ) : (
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {media.map((item) => (
            <li
              key={item.id}
              className="group relative aspect-video overflow-hidden rounded-xl border border-border bg-muted"
            >
              <button
                type="button"
                onClick={() => setViewing(item)}
                className="block size-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                aria-label={`View ${item.fileName}`}
              >
                {item.kind === "Image" ? (
                  <img
                    src={mediaUrl(item.url)}
                    alt={item.fileName}
                    loading="lazy"
                    className="size-full object-cover transition-transform group-hover:scale-[1.02]"
                  />
                ) : (
                  <>
                    <video
                      src={mediaUrl(item.url)}
                      preload="metadata"
                      muted
                      className="size-full object-cover"
                    />
                    <span className="absolute inset-0 flex items-center justify-center">
                      <span className="flex size-10 items-center justify-center rounded-full bg-black/60 text-white">
                        <Play className="size-5 translate-x-px" aria-hidden />
                      </span>
                    </span>
                  </>
                )}
              </button>
              <Button
                type="button"
                variant="secondary"
                size="icon"
                className="absolute right-2 top-2 size-8 opacity-0 shadow transition-opacity focus-visible:opacity-100 group-hover:opacity-100"
                onClick={() => setDeleting(item)}
                aria-label={`Remove ${item.fileName}`}
              >
                <Trash2 className="size-4" aria-hidden />
              </Button>
            </li>
          ))}
        </ul>
      )}

      <Dialog open={viewing !== null} onOpenChange={(open) => !open && setViewing(null)}>
        <DialogContent className="max-w-4xl p-2 sm:p-3">
          <DialogTitle className="sr-only">{viewing?.fileName}</DialogTitle>
          {viewing?.kind === "Image" && (
            <img
              src={mediaUrl(viewing.url)}
              alt={viewing.fileName}
              className="max-h-[80vh] w-full rounded-lg object-contain"
            />
          )}
          {viewing?.kind === "Video" && (
            <video
              src={mediaUrl(viewing.url)}
              controls
              autoPlay
              className="max-h-[80vh] w-full rounded-lg bg-black"
            />
          )}
        </DialogContent>
      </Dialog>

      <AlertDialog open={deleting !== null} onOpenChange={(open) => !open && setDeleting(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove this {deleting?.kind === "Video" ? "video" : "photo"}?</AlertDialogTitle>
            <AlertDialogDescription>
              {deleting?.fileName} will be deleted from this supplier's profile. This can't be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={confirmDelete}>Remove</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  );
}
