import { useMutation } from "@tanstack/react-query";

import { extractSupplier } from "@/api/client";
import type { ExtractSupplierResponse } from "@/api/types";

export function useExtractSupplier() {
  return useMutation<ExtractSupplierResponse, unknown, string>({
    mutationFn: extractSupplier,
  });
}
