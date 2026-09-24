import { useMutation, useQueryClient } from "@tanstack/react-query";

import { createSupplier } from "@/api/client";
import type { CreateSupplierRequest, Supplier } from "@/api/types";

export function useCreateSupplier() {
  const queryClient = useQueryClient();

  return useMutation<Supplier, unknown, CreateSupplierRequest>({
    mutationFn: createSupplier,
    onSuccess: (supplier) => {
      queryClient.invalidateQueries({ queryKey: ["suppliers"] });
      queryClient.setQueryData(["suppliers", "detail", supplier.id], supplier);
    },
  });
}
