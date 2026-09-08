import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { platformContactRequestsApi } from "../api/contactRequests.api";
import type { ContactRequestStatus } from "../types/contactRequests.types";
const keys = { all: ["platform", "contactRequests"] as const, list: (page: number, size: number, status: string) => ["platform", "contactRequests", "list", page, size, status] as const, detail: (id: string) => ["platform", "contactRequests", "detail", id] as const };
export function useContactRequests(page: number, size: number, status: string) { return useQuery({ queryKey: keys.list(page, size, status), queryFn: () => platformContactRequestsApi.list(page, size, status || undefined) }); }
export function useContactRequest(id: string | null) { return useQuery({ queryKey: keys.detail(id ?? ""), queryFn: () => platformContactRequestsApi.detail(id!), enabled: Boolean(id) }); }
export function useUpdateContactRequestStatus() { const client = useQueryClient(); return useMutation({ mutationFn: ({ id, status }: { id: string; status: ContactRequestStatus }) => platformContactRequestsApi.updateStatus(id, status), onSuccess: (result) => { client.setQueryData(keys.detail(result.id), result); client.invalidateQueries({ queryKey: keys.all }); } }); }
