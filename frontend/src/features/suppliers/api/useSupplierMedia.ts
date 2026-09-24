import { useMutation, useQueryClient } from "@tanstack/react-query";

import { deleteSupplierMedia, uploadSupplierMedia, type UploadProgress } from "@/api/client";
import type { Media } from "@/api/types";

/** Refreshes the supplier (its media list) and the list page (its cover image). */
function useInvalidateSupplier(supplierId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ["suppliers", "detail", supplierId] });
    queryClient.invalidateQueries({ queryKey: ["suppliers", "list"] });
  };
}

export function useUploadSupplierMedia(supplierId: string) {
  const invalidate = useInvalidateSupplier(supplierId);
  return useMutation<Media, unknown, { file: File; onProgress?: UploadProgress }>({
    mutationFn: ({ file, onProgress }) => uploadSupplierMedia(supplierId, file, onProgress),
    onSettled: invalidate,
  });
}

export function useDeleteSupplierMedia(supplierId: string) {
  const invalidate = useInvalidateSupplier(supplierId);
  return useMutation<void, unknown, string>({
    mutationFn: (mediaId) => deleteSupplierMedia(supplierId, mediaId),
    onSettled: invalidate,
  });
}
