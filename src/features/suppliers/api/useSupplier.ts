import { queryOptions, useQuery } from "@tanstack/react-query";

import { ApiError, getSupplier } from "@/api/client";

export const supplierQueryOptions = (id: string) =>
  queryOptions({
    queryKey: ["suppliers", "detail", id],
    queryFn: () => getSupplier(id),
    retry: (failureCount, error) => {
      if (error instanceof ApiError && error.status < 500) return false;
      return failureCount < 2;
    },
  });

export function useSupplier(id: string) {
  return useQuery(supplierQueryOptions(id));
}
