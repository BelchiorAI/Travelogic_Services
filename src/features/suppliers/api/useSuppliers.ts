import { keepPreviousData, queryOptions, useQuery } from "@tanstack/react-query";

import { getSuppliers } from "@/api/client";
import type { SupplierListParams } from "@/api/types";

export const suppliersQueryOptions = (params: SupplierListParams) =>
  queryOptions({
    queryKey: ["suppliers", "list", params],
    queryFn: () => getSuppliers(params),
    placeholderData: keepPreviousData,
  });

export function useSuppliers(params: SupplierListParams) {
  return useQuery(suppliersQueryOptions(params));
}
