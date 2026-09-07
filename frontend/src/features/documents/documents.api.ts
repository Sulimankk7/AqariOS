import { http } from "@/shared/lib/http";
import { resolveSignedFileUrl } from "@/shared/services/files.api";
import { z } from "zod";
export const categorySchema = z.object({
  name: z.string().trim().min(1).max(100),
  description: z.string().max(255).nullable(),
});
export interface Category {
  id: string;
  name: string;
  description?: string | null;
}
export interface BuildingDocument {
  companyId?: string;
  id: string;
  buildingId: string;
  categoryId: string;
  categoryName: string;
  documentName: string;
  issueDate?: string;
  expiryDate?: string;
  isConfidential: boolean;
  createdAt: string;
  originalFilename?: string;
  mimeType?: string;
  sizeBytes?: number;
  description?: string;
  uploadedBy?: string;
}
export const documentMetadataSchema = z
  .object({
    documentName: z.string().trim().min(1).max(255),
    description: z.string().max(2000).nullable(),
    issueDate: z.string().nullable(),
    expiryDate: z.string().nullable(),
    isConfidential: z.boolean(),
  })
  .refine((v) => !v.issueDate || !v.expiryDate || v.expiryDate > v.issueDate, {
    path: ["expiryDate"],
    message: "Expiry must follow issue date",
  });
export type DocumentMetadata = z.infer<typeof documentMetadataSchema>;
export interface DocumentPage {
  items: BuildingDocument[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
}
export interface DocumentFilters {
  categoryId?: string;
  searchTerm?: string;
  pageNumber: number;
  pageSize: number;
}
export function safeDocumentUrl(value: string): string {
  return resolveSignedFileUrl(value);
}
export const documentsApi = {
  create: (
    buildingId: string,
    body: DocumentMetadata & { categoryId: string; fileId: string },
  ) =>
    http.post<BuildingDocument>(
      `/api/v1/buildings/${buildingId}/documents`,
      body,
    ),
  update: (id: string, body: DocumentMetadata) =>
    http.put<BuildingDocument>(`/api/v1/building-documents/${id}`, body),
  replace: (id: string, newFileId: string) =>
    http.post<BuildingDocument>(`/api/v1/building-documents/${id}/replace`, {
      newFileId,
    }),
  remove: (id: string) => http.delete<void>(`/api/v1/building-documents/${id}`),
  categories: () => http.get<Category[]>("/api/v1/document-categories"),
  createCategory: (body: z.infer<typeof categorySchema>) =>
    http.post<Category>("/api/v1/document-categories", body),
  updateCategory: (id: string, body: z.infer<typeof categorySchema>) =>
    http.put<Category>(`/api/v1/document-categories/${id}`, body),
  deleteCategory: (id: string) =>
    http.delete<void>(`/api/v1/document-categories/${id}`),
  list: (buildingId: string, filters: DocumentFilters) => {
    const params = new URLSearchParams({
      pageNumber: String(filters.pageNumber),
      pageSize: String(filters.pageSize),
    });
    if (filters.categoryId) params.set("categoryId", filters.categoryId);
    if (filters.searchTerm) params.set("searchTerm", filters.searchTerm);
    return http.get<DocumentPage>(
      `/api/v1/buildings/${buildingId}/documents?${params}`,
    );
  },
  detail: (id: string) =>
    http.get<BuildingDocument>(`/api/v1/building-documents/${id}`),
  download: async (id: string) => {
    const result = await http.get<{ downloadUrl: string }>(
      `/api/v1/building-documents/${id}/download-url`,
    );
    return safeDocumentUrl(result.downloadUrl);
  },
};
