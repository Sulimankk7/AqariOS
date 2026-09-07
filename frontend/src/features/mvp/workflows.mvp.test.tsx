import React from "react";
import { apiUrl } from "@/config/api";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  render,
  screen,
  fireEvent,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router";
import { I18nProvider } from "@/shared/i18n";
import { ThemeProvider } from "@/shared/theme/ThemeProvider";
import { http, ApiError } from "@/shared/lib/http";
import ParkingPage from "../parking/ParkingPage";
import DocumentsPage from "../documents/DocumentsPage";
import NotificationsPage from "../notifications/pages/NotificationsPage";
import { NotificationBell } from "../notifications/components/NotificationBell";
import SettingsPage from "../settings/SettingsPage";
import { parkingSchema } from "../parking/parking.api";
import { safeDocumentUrl } from "../documents/documents.api";
import { settingsSchema } from "../settings/settings.api";
import { canMarkRead } from "../notifications/utils/inbox";
import { filesApi } from "@/shared/services/files.api";
import { documentMetadataSchema } from "../documents/documents.api";

describe("Signed building-document transfer", () => {
  it("resolves API-relative capabilities without modifying absolute cloud URLs", () => {
    expect(safeDocumentUrl("/api/v1/files/download?sig=test-only")).toBe(
      new URL(
        apiUrl("/api/v1/files/download?sig=test-only"),
        window.location.origin,
      ).href,
    );
    expect(
      safeDocumentUrl("https://storage.example.test/a?sig=test-only"),
    ).toBe("https://storage.example.test/a?sig=test-only");
    expect(() => safeDocumentUrl("//untrusted.example.test/file")).toThrow();
  });
  it("uses direct PUT and only storage content headers, without application authorization", async () => {
    const headers: Record<string, string> = {};
    let opened = "";
    let method = "";
    let sent: unknown;
    class Transfer {
      status = 204;
      upload = {};
      onload?: () => void;
      onerror?: () => void;
      ontimeout?: () => void;
      open(m: string, url: string) {
        method = m;
        opened = url;
      }
      setRequestHeader(key: string, value: string) {
        headers[key] = value;
      }
      send(file: unknown) {
        sent = file;
        this.onload?.();
      }
    }
    vi.stubGlobal("XMLHttpRequest", Transfer);
    const file = new File(["pdf"], "test.pdf", { type: "application/pdf" });
    try {
      await filesApi.uploadBinary("/api/v1/files/upload?sig=test-only", file);
      expect(method).toBe("PUT");
      expect(opened).toBe(
        new URL(
          apiUrl("/api/v1/files/upload?sig=test-only"),
          window.location.origin,
        ).href,
      );
      expect(headers).toEqual({ "Content-Type": "application/pdf" });
      expect(sent).toBe(file);
    } finally {
      vi.unstubAllGlobals();
    }
  });
});

const auth = vi.hoisted(() => ({
  user: {
    id: "user",
    companyId: "company",
    permissions: ["properties.manage"],
    name: "Owner",
    email: "owner@example.test",
  },
  isAuthenticated: true,
  logout: vi.fn(),
}));
vi.mock("@/features/auth/hooks/useAuth", () => ({ useAuth: () => auth }));
vi.mock("@/shared/lib/http", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/shared/lib/http")>();
  return {
    ...actual,
    http: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      patch: vi.fn(),
      delete: vi.fn(),
    },
  };
});
const building = {
  id: "building",
  companyId: "company",
  name: "مبنى طويل Long building name",
  isActive: true,
};
const spot = {
  id: "spot",
  buildingId: "building",
  companyId: "company",
  spotCode: "P-01",
  parkingType: 0,
  defaultApartmentId: null,
  locationDescription: "Entrance",
  isActive: true,
};
const document = {
  id: "doc",
  buildingId: "building",
  categoryId: "category",
  categoryName: "Licenses",
  documentName: "Building license",
  createdAt: "2026-09-01T00:00:00Z",
  isConfidential: false,
  originalFilename: "license.pdf",
  mimeType: "application/pdf",
  sizeBytes: 500,
  description: "Long document description",
};
const company = {
  id: "company",
  legalName: "Company Ltd",
  displayName: "Aqari",
  primaryPhone: "+962790000000",
  primaryEmail: "owner@example.test",
  countryCode: "JO",
  companyType: 0,
  isActive: true,
};
const settings = {
  rentGracePeriodDays: 5,
  lateFeeType: 0,
  lateFeeValue: null,
  fiscalYearStartMonth: 1,
  defaultCurrency: "JOD",
  defaultLanguage: "ar",
  timezone: "Asia/Amman",
};
const notification = {
  id: "n1",
  subject: "Rent due",
  body: "Payment reminder body",
  status: 1,
  readAt: null,
  notificationType: 2,
  priority: 1,
  createdAt: "2026-09-01T00:00:00Z",
};
let pages = false;
function reads(url: string): unknown {
  if (url === "/api/v1/buildings") return [building];
  if (url.startsWith("/api/v1/apartments")) return [];
  if (url === "/api/v1/leasing/tenants") return [{ id: "tenant-1", name: "Tenant One" }];
  if (url.endsWith("/parking-spots")) return [spot];
  if (url === "/api/v1/document-categories")
    return [
      { id: "category", name: "Licenses", description: "Official records" },
    ];
  if (url.includes("/documents?"))
    return {
      items: [document],
      pageNumber: Number(
        new URL(url, "http://test").searchParams.get("pageNumber"),
      ),
      pageSize: 50,
      totalCount: 100,
      hasNextPage: true,
    };
  if (url === "/api/v1/building-documents/doc") return document;
  if (url.endsWith("/download-url"))
    return {
      downloadUrl: "https://storage.example.test/license?signature=test-only",
    };
  if (url.endsWith("/unread-count")) return 1;
  if (url.startsWith("/api/v1/notifications/me"))
    return pages
      ? Array.from({ length: 50 }, (_, i) => ({
          ...notification,
          id: `n${i}`,
          subject: `Notice ${i}`,
        }))
      : [
          notification,
          {
            ...notification,
            id: "pending",
            subject: "Pending notice",
            status: 0,
          },
        ];
  if (url === "/api/v1/companies/me") return company;
  if (url.endsWith("/settings")) return settings;
  throw new Error(`Unexpected API: ${url}`);
}
beforeEach(() => {
  vi.clearAllMocks();
  pages = false;
  auth.user.permissions = ["properties.manage"];
  localStorage.clear();
  localStorage.setItem("aqarios_lang", "en");
  vi.mocked(http.get).mockImplementation(async (url) => reads(url) as never);
  vi.mocked(http.post).mockResolvedValue(undefined);
  vi.mocked(http.put).mockResolvedValue(undefined);
  vi.mocked(http.delete).mockResolvedValue(undefined);
  vi.mocked(http.patch).mockResolvedValue({ markedCount: 1 });
});
function mount(element: React.ReactNode) {
  const cache = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={cache}>
      <MemoryRouter>
        <I18nProvider defaultLanguage="en">
          <ThemeProvider>{element}</ThemeProvider>
        </I18nProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}
async function choose(label: string, option: string) {
  await userEvent.click(await screen.findByRole("combobox", { name: label }));
  await userEvent.click(await screen.findByRole("option", { name: option }));
}
async function chooseBuilding() {
  await choose("Building", building.name);
}

describe("Parking", () => {
  it("loads only selected building parking and edits supported fields", async () => {
    mount(<ParkingPage />);
    expect(http.get).not.toHaveBeenCalledWith(
      expect.stringContaining("/parking-spots"),
    );
    await chooseBuilding();
    await screen.findByText("P-01");
    await userEvent.click(screen.getByRole("button", { name: "Edit" }));
    const dialog = screen.getByRole("dialog");
    await userEvent.clear(within(dialog).getByLabelText("Spot code"));
    await userEvent.type(within(dialog).getByLabelText("Spot code"), "P-02");
    await userEvent.click(within(dialog).getByRole("button", { name: "Save" }));
    await waitFor(() =>
      expect(http.put).toHaveBeenCalledWith(
        "/api/v1/parking-spots/spot",
        expect.objectContaining({ spotCode: "P-02", defaultApartmentId: null }),
      ),
    );
  });
  it("suggests an editable code and submits a valid parking spot", async () => {
    expect(
      parkingSchema.safeParse({
        spotCode: " ",
        parkingType: 9,
        defaultApartmentId: null,
        locationDescription: "",
      }).success,
    ).toBe(false);
    mount(<ParkingPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: "Create" }),
    );
    const dialog = screen.getByRole("dialog");
    expect((within(dialog).getByLabelText("Spot code") as HTMLInputElement).value).toBe("P-02");
    await userEvent.click(within(dialog).getByRole("combobox", { name: "Parking type" }));
    expect(await screen.findByRole("option", { name: "Standard" })).toBeTruthy();
    expect(screen.getByRole("option", { name: "Disabled access" })).toBeTruthy();
    expect(screen.queryByRole("option", { name: "Covered" })).toBeNull();
    await userEvent.keyboard("{Escape}");
    await userEvent.click(within(dialog).getByRole("button", { name: "Save" }));
    await waitFor(() =>
      expect(http.post).toHaveBeenCalledWith(
        "/api/v1/buildings/building/parking-spots",
        expect.objectContaining({ spotCode: "P-02", parkingType: 0 }),
      ),
    );
  });
  it("requires confirmation and renders archive dependencies", async () => {
    vi.mocked(http.delete).mockRejectedValue(
      new ApiError(409, "Blocked", {
        code: "ARCHIVE_BLOCKED",
        title: "ParkingSpot cannot be archived",
        dependencies: [
          {
            code: "PARKING_ASSIGNMENTS",
            label: "Parking Assignments",
            count: 2,
          },
        ],
      }),
    );
    mount(<ParkingPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: "Archive" }),
    );
    expect(http.delete).not.toHaveBeenCalled();
    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await waitFor(() =>
      expect(http.delete).toHaveBeenCalledWith("/api/v1/parking-spots/spot"),
    );
    await screen.findByRole("alertdialog");
    expect(screen.queryByRole("button", { name: /assign/i })).toBeNull();
  });
  it("hides mutation actions for read-only permission", async () => {
    auth.user.permissions = ["properties.read"];
    mount(<ParkingPage />);
    await chooseBuilding();
    await screen.findByText("P-01");
    expect(screen.queryByRole("button", { name: "Create" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Archive" })).toBeNull();
  });
  it("treats a 204 current assignment as unassigned and assigns an eligible active lease", async () => {
    vi.mocked(http.get).mockImplementation(async (url) => {
      if (url === "/api/v1/parking-spots/spot/assignment") return undefined as never;
      if (url.startsWith("/api/v1/leasing/contracts/search")) return [{ id: "lease-active", buildingId: "building", tenantId: "tenant-1", contractNumber: "LC-101", status: 2 }] as never;
      return reads(url) as never;
    });
    mount(<ParkingPage />);
    await chooseBuilding();
    await userEvent.click(await screen.findByRole("button", { name: "Details" }));
    await screen.findByText("This parking spot has no active assignment.");
    await choose("Active lease contract", "LC-101 — Tenant One");
    await userEvent.click(screen.getByRole("button", { name: "Assign to lease" }));
    await waitFor(() => expect(http.post).toHaveBeenCalledWith("/api/v1/parking-spots/spot/assignment", { leaseContractId: "lease-active" }));
  });
  it("ends the current assignment only after confirmation", async () => {
    vi.mocked(http.get).mockImplementation(async (url) => {
      if (url === "/api/v1/parking-spots/spot/assignment") return { assignmentId: "assignment-1", parkingSpotId: "spot", parkingSpotCode: "P-01", parkingType: 0, location: "Entrance", leaseContractId: "lease-active", tenantId: "tenant-1", tenantName: "Tenant One", startDate: "2026-09-01", endDate: null, status: 0 } as never;
      if (url === "/api/v1/leasing/contracts/lease-active") return { id: "lease-active", contractNumber: "LC-101" } as never;
      return reads(url) as never;
    });
    mount(<ParkingPage />);
    await chooseBuilding();
    await userEvent.click(await screen.findByRole("button", { name: "Details" }));
    await screen.findByText("Tenant One");
    expect(await screen.findByText("LC-101")).toBeTruthy();
    expect(screen.queryByText("lease-active")).toBeNull();
    await userEvent.click(screen.getByRole("button", { name: "End assignment" }));
    expect(http.post).not.toHaveBeenCalledWith("/api/v1/parking-assignments/assignment-1/end");
    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await waitFor(() => expect(http.post).toHaveBeenCalledWith("/api/v1/parking-assignments/assignment-1/end"));
  });
});
describe("Documents", () => {
  async function openCreate() {
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(screen.getByRole("button", { name: "Add document" }));
    await choose("Document categories", "Licenses");
    await userEvent.type(
      within(screen.getByRole("dialog")).getByLabelText("Document name"),
      "Insurance",
    );
  }
  function uploadResponses() {
    vi.spyOn(filesApi, "uploadBinary").mockImplementation(
      async (_url, _file, onProgress) => onProgress?.(100),
    );
    vi.mocked(http.post).mockImplementation(async (url) => {
      if (url.endsWith("/upload-request"))
        return {
          fileId: "file",
          storageKey: "key",
          uploadUrl: "https://storage.example.test/upload",
          expirationMinutes: 15,
        } as never;
      if (url.endsWith("/confirm")) return { id: "file" } as never;
      if (url.endsWith("/replace"))
        return {
          ...document,
          id: "replacement",
          documentName: "Replacement",
        } as never;
      return { ...document, documentName: "Insurance" } as never;
    });
  }
  const uploadFile = () =>
    userEvent.upload(
      screen.getByLabelText("File"),
      new File(["test document"], "insurance.txt", { type: "text/plain" }),
    );
  it("selects, removes and reselects a file without uploading before Save", async () => {
    uploadResponses();
    await openCreate();
    expect(screen.queryByLabelText("Confidential")).toBeNull();
    const picker = screen.getByLabelText("File") as HTMLInputElement;
    const click = vi.spyOn(picker, "click");
    await userEvent.click(
      screen.getByRole("button", { name: /Choose a file/ }),
    );
    expect(click).toHaveBeenCalledOnce();
    await uploadFile();
    expect(screen.getByText("insurance.txt")).toBeTruthy();
    await userEvent.click(
      screen.getByRole("button", { name: "Remove selected file" }),
    );
    expect(screen.queryByText("insurance.txt")).toBeNull();
    expect(picker.value).toBe("");
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    expect(await screen.findByText("Select a non-empty file.")).toBeTruthy();
    await uploadFile();
    expect(screen.queryByText("Select a non-empty file.")).toBeNull();
    expect(screen.getByText("insurance.txt")).toBeTruthy();
    expect(http.post).not.toHaveBeenCalled();
    expect(filesApi.uploadBinary).not.toHaveBeenCalled();
  });
  it("creates through request, binary, confirm and attachment in order with progress and selected building", async () => {
    uploadResponses();
    await openCreate();
    await uploadFile();
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await waitFor(() =>
      expect(http.post).toHaveBeenCalledWith(
        "/api/v1/buildings/building/documents",
        {
          categoryId: "category",
          fileId: "file",
          documentName: "Insurance",
          description: null,
          issueDate: null,
          expiryDate: null,
          isConfidential: false,
        },
      ),
    );
    expect(vi.mocked(http.post).mock.calls.map((c) => c[0])).toEqual([
      "/api/v1/files/upload-request",
      "/api/v1/files/confirm",
      "/api/v1/buildings/building/documents",
    ]);
    expect(http.post).toHaveBeenCalledWith(
      "/api/v1/files/upload-request",
      expect.objectContaining({
        moduleName: "BuildingDocuments",
        entityId: "building",
        mimeType: "text/plain",
      }),
    );
    expect(http.post).toHaveBeenCalledWith(
      "/api/v1/files/confirm",
      expect.objectContaining({
        fileId: "file",
        storageKey: "key",
        originalFilename: "insurance.txt",
      }),
    );
    expect(filesApi.uploadBinary).toHaveBeenCalledWith(
      "https://storage.example.test/upload",
      expect.any(File),
      expect.any(Function),
    );
    expect(
      vi.mocked(filesApi.uploadBinary).mock.invocationCallOrder[0],
    ).toBeGreaterThan(vi.mocked(http.post).mock.invocationCallOrder[0]);
    expect(
      vi.mocked(filesApi.uploadBinary).mock.invocationCallOrder[0],
    ).toBeLessThan(vi.mocked(http.post).mock.invocationCallOrder[1]);
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(
      screen.getByRole("combobox", { name: "Building" }).textContent,
    ).toContain(building.name);
  });
  it("blocks double submission while uploading and resumes after failed confirmation without another binary transfer", async () => {
    uploadResponses();
    let finish!: (value: void) => void;
    vi.mocked(filesApi.uploadBinary).mockImplementationOnce(
      () =>
        new Promise<void>((resolve) => {
          finish = resolve;
        }),
    );
    vi.mocked(http.post).mockImplementation(async (url) => {
      if (url.endsWith("/upload-request"))
        return {
          fileId: "file",
          storageKey: "key",
          uploadUrl: "https://storage.example.test/upload",
        } as never;
      if (url.endsWith("/confirm")) throw new ApiError(503, "Unavailable");
      return document as never;
    });
    await openCreate();
    await uploadFile();
    const form = screen.getByLabelText("File").closest("form")!;
    fireEvent.submit(form);
    fireEvent.submit(form);
    await screen.findByRole("progressbar");
    expect(
      vi
        .mocked(http.post)
        .mock.calls.filter((c) => c[0].endsWith("/upload-request")),
    ).toHaveLength(1);
    expect(
      within(screen.getByRole("dialog"))
        .getByRole("button", { name: "Save" })
        .hasAttribute("disabled"),
    ).toBe(true);
    finish();
    await screen.findByRole("alert");
    vi.mocked(http.post).mockImplementation(
      async (url) =>
        (url.endsWith("/confirm") ? { id: "file" } : document) as never,
    );
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await waitFor(() =>
      expect(http.post).toHaveBeenCalledWith(
        "/api/v1/buildings/building/documents",
        expect.any(Object),
      ),
    );
    expect(filesApi.uploadBinary).toHaveBeenCalledTimes(1);
  });
  it("does not register a document after a binary upload failure", async () => {
    uploadResponses();
    vi.mocked(filesApi.uploadBinary).mockRejectedValue(
      new Error("Network error"),
    );
    await openCreate();
    await uploadFile();
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await screen.findByRole("alert");
    expect(http.post).not.toHaveBeenCalledWith(
      "/api/v1/files/confirm",
      expect.anything(),
    );
    expect(http.post).not.toHaveBeenCalledWith(
      "/api/v1/buildings/building/documents",
      expect.anything(),
    );
  });
  it("retries attachment failure using the already confirmed file", async () => {
    uploadResponses();
    const original = vi.mocked(http.post).getMockImplementation()!;
    vi.mocked(http.post).mockImplementation(async (url, body) => {
      if (url.endsWith("/documents"))
        throw new ApiError(422, "Invalid", {
          detail: "Document name rejected",
        });
      return original(url, body) as never;
    });
    await openCreate();
    await uploadFile();
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await screen.findByRole("alert");
    vi.mocked(http.post).mockResolvedValue(document);
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(
      vi.mocked(http.post).mock.calls.filter((c) => c[0].endsWith("/confirm")),
    ).toHaveLength(1);
    expect(filesApi.uploadBinary).toHaveBeenCalledTimes(1);
  });
  it("updates metadata while preserving hidden confidentiality, without uploading or category reassignment", async () => {
    vi.mocked(http.get).mockImplementation(
      async (url) =>
        (url === "/api/v1/building-documents/doc"
          ? { ...document, isConfidential: true }
          : reads(url)) as never,
    );
    vi.mocked(http.put).mockResolvedValue({
      ...document,
      documentName: "Edited",
    });
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Edit metadata" }),
    );
    const dialog = screen.getByRole("dialog", { name: "Edit metadata" });
    await userEvent.clear(within(dialog).getByLabelText("Document name"));
    await userEvent.type(
      within(dialog).getByLabelText("Document name"),
      "Edited",
    );
    await userEvent.clear(within(dialog).getByLabelText("Description"));
    expect(within(dialog).queryByLabelText("Confidential")).toBeNull();
    expect(screen.queryByText("Confidential")).toBeNull();
    expect(
      within(dialog).queryByRole("combobox", { name: "Document categories" }),
    ).toBeNull();
    await userEvent.click(within(dialog).getByRole("button", { name: "Save" }));
    await waitFor(() =>
      expect(http.put).toHaveBeenCalledWith("/api/v1/building-documents/doc", {
        documentName: "Edited",
        description: null,
        issueDate: null,
        expiryDate: null,
        isConfidential: true,
      }),
    );
    expect(http.post).not.toHaveBeenCalled();
  });
  it("replaces the file using newFileId then loads the returned document identity", async () => {
    uploadResponses();
    vi.mocked(http.get).mockImplementation(
      async (url) =>
        (url === "/api/v1/building-documents/replacement"
          ? { ...document, id: "replacement", documentName: "Replacement" }
          : reads(url)) as never,
    );
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Replace file" }),
    );
    await uploadFile();
    await userEvent.click(
      within(screen.getByRole("dialog", { name: "Replace file" })).getByRole(
        "button",
        { name: "Save" },
      ),
    );
    await waitFor(() =>
      expect(http.post).toHaveBeenCalledWith(
        "/api/v1/building-documents/doc/replace",
        { newFileId: "file" },
      ),
    );
    await waitFor(() =>
      expect(http.get).toHaveBeenCalledWith(
        "/api/v1/building-documents/replacement",
      ),
    );
  });
  it("requires document removal confirmation, preserves failure, then closes details on success", async () => {
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Delete document" }),
    );
    expect(http.delete).not.toHaveBeenCalled();
    vi.mocked(http.delete).mockRejectedValueOnce(
      new ApiError(409, "Conflict", { detail: "Document is in use" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await screen.findByRole("alert");
    expect(
      screen.getByRole("dialog", { name: "Delete document" }),
    ).toBeTruthy();
    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(http.delete).toHaveBeenCalledWith("/api/v1/building-documents/doc");
  });
  it("hides document and category mutations without the corresponding permission", async () => {
    auth.user.permissions = ["properties.read"];
    mount(<DocumentsPage />);
    await chooseBuilding();
    expect(screen.queryByRole("button", { name: "Add document" })).toBeNull();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await screen.findByRole("button", { name: "Download" });
    for (const name of ["Edit metadata", "Replace file", "Delete document"])
      expect(screen.queryByRole("button", { name })).toBeNull();
    await userEvent.click(screen.getByRole("button", { name: "Close" }));
    await userEvent.click(
      screen.getByRole("tab", { name: "Document categories" }),
    );
    await screen.findByText("Licenses");
    expect(screen.queryByRole("button", { name: "Create" })).toBeNull();
  });
  it("validates required files and metadata date/length constraints before submission", async () => {
    await openCreate();
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await screen.findByText("Select a non-empty file.");
    expect(http.post).not.toHaveBeenCalled();
    expect(
      documentMetadataSchema.safeParse({
        documentName: "x".repeat(256),
        description: null,
        issueDate: null,
        expiryDate: null,
        isConfidential: false,
      }).success,
    ).toBe(false);
    expect(
      documentMetadataSchema.safeParse({
        documentName: "Valid",
        description: null,
        issueDate: "2026-01-02",
        expiryDate: "2026-01-01",
        isConfidential: false,
      }).success,
    ).toBe(false);
  });
  it.each([false, true])(
    "distinguishes building-empty and search-empty states (search=%s)",
    async (search) => {
      vi.mocked(http.get).mockImplementation(async (url) =>
        url.includes("/documents?")
          ? ({ items: [], hasNextPage: false } as never)
          : (reads(url) as never),
      );
      mount(<DocumentsPage />);
      await chooseBuilding();
      await screen.findByText("No documents for this building.");
      if (search) {
        await userEvent.type(screen.getByLabelText("Document name"), "nothing");
        await userEvent.click(screen.getByRole("button", { name: "Search" }));
        await screen.findByText("No matching documents.");
      }
    },
  );
  it("shows a separate no-buildings state", async () => {
    vi.mocked(http.get).mockResolvedValue([]);
    mount(<DocumentsPage />);
    await screen.findByText("No buildings available.");
    expect(screen.queryByRole("button", { name: "Add document" })).toBeNull();
  });
  it("sends server search, category and page-number parameters", async () => {
    mount(<DocumentsPage />);
    await chooseBuilding();
    await screen.findByText("Building license");
    await userEvent.type(screen.getByLabelText("Document name"), "رخصة");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));
    await choose("Document categories", "Licenses");
    await userEvent.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = vi.mocked(http.get).mock.calls.map((c) => c[0]);
      expect(
        urls.some((url) => {
          const p = new URL(url, "http://test").searchParams;
          return (
            p.get("pageNumber") === "2" &&
            p.get("categoryId") === "category" &&
            p.get("searchTerm") === "رخصة"
          );
        }),
      ).toBe(true);
    });
    await userEvent.click(screen.getByRole("button", { name: "Previous" }));
  });
  it("loads details and uses only document-specific signed download", async () => {
    const open = vi.spyOn(window, "open").mockReturnValue(null);
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await screen.findByText("license.pdf");
    await userEvent.click(screen.getByRole("button", { name: "Download" }));
    await waitFor(() =>
      expect(open).toHaveBeenCalledWith(
        "https://storage.example.test/license?signature=test-only",
        "_blank",
        "noopener,noreferrer",
      ),
    );
    expect(
      vi
        .mocked(http.get)
        .mock.calls.some((c) => c[0].startsWith("/api/v1/files/")),
    ).toBe(false);
  });
  it("preserves denied detail and never falls back to generic file API", async () => {
    vi.mocked(http.get).mockImplementation(async (url) => {
      if (url === "/api/v1/building-documents/doc")
        throw new ApiError(401, "Confidential", { detail: "Access denied" });
      return reads(url) as never;
    });
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await screen.findByText("You do not have access to this resource.");
    expect(screen.queryByRole("button", { name: "Download" })).toBeNull();
    expect(
      vi
        .mocked(http.get)
        .mock.calls.filter((c) => c[0] === "/api/v1/building-documents/doc"),
    ).toHaveLength(1);
  });
  it("creates and edits categories and preserves category-in-use deletion", async () => {
    mount(<DocumentsPage />);
    await userEvent.click(
      screen.getByRole("tab", { name: "Document categories" }),
    );
    await screen.findByText("Licenses");
    await userEvent.click(screen.getByRole("button", { name: "Create" }));
    await userEvent.type(screen.getByLabelText("Name"), "Insurance");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() =>
      expect(http.post).toHaveBeenCalledWith("/api/v1/document-categories", {
        name: "Insurance",
        description: null,
      }),
    );
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    await userEvent.click(screen.getByRole("button", { name: "Edit" }));
    await userEvent.clear(screen.getByLabelText("Name"));
    await userEvent.type(screen.getByLabelText("Name"), "Permits");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() =>
      expect(http.put).toHaveBeenCalledWith(
        "/api/v1/document-categories/category",
        expect.objectContaining({ name: "Permits" }),
      ),
    );
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    vi.mocked(http.delete).mockRejectedValue(
      new ApiError(422, "In use", { code: "DOCUMENT_CATEGORY_IN_USE" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(http.delete).not.toHaveBeenCalled();
    await userEvent.click(screen.getByRole("button", { name: "Confirm" }));
    await screen.findByText(
      "This category is referenced by building documents and cannot be deleted.",
    );
  });
  it("rejects executable and credential-bearing download URLs", () => {
    expect(() => safeDocumentUrl("javascript:alert(1)")).toThrow();
    expect(() => safeDocumentUrl("https://user:pass@example.test/a")).toThrow();
  });
  it("preserves file-validation errors before any binary transfer", async () => {
    uploadResponses();
    vi.mocked(http.post).mockRejectedValue(
      new ApiError(422, "Invalid file", {
        code: "FILE_VALIDATION_FAILED",
        detail: "File type is not allowed.",
      }),
    );
    await openCreate();
    await uploadFile();
    await userEvent.click(
      within(screen.getByRole("dialog")).getByRole("button", { name: "Save" }),
    );
    await screen.findByText("File type is not allowed.");
    expect(filesApi.uploadBinary).not.toHaveBeenCalled();
  });
  it("preserves forbidden metadata response without trying another endpoint", async () => {
    vi.mocked(http.put).mockRejectedValue(new ApiError(403, "Forbidden"));
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Edit metadata" }),
    );
    await userEvent.click(
      within(screen.getByRole("dialog", { name: "Edit metadata" })).getByRole(
        "button",
        { name: "Save" },
      ),
    );
    await screen.findByText("You do not have access to this resource.");
    expect(http.put).toHaveBeenCalledTimes(1);
    expect(http.post).not.toHaveBeenCalled();
  });
  it("offers a user-initiated safe link when the download popup is blocked", async () => {
    vi.spyOn(window, "open").mockReturnValue(null);
    mount(<DocumentsPage />);
    await chooseBuilding();
    await userEvent.click(
      await screen.findByRole("button", { name: /Building license/ }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Download" }),
    );
    const link = await screen.findByRole("link", { name: "Open file" });
    expect(link.getAttribute("href")).toBe(
      "https://storage.example.test/license?signature=test-only",
    );
    expect(link.getAttribute("rel")).toBe("noopener noreferrer");
  });
});
describe("Notifications", () => {
  it("keeps pending bell notifications unreadable and labels delivery status accurately", async () => {
    mount(<NotificationBell />);
    await userEvent.click(
      screen.getByRole("button", { name: "Notifications" }),
    );
    await userEvent.click(
      await screen.findByRole("button", { name: "Pending notice - Pending" }),
    );
    expect(http.patch).not.toHaveBeenCalled();
    await userEvent.click(
      screen.getByRole("button", { name: "Rent due - Sent" }),
    );
    await waitFor(() =>
      expect(http.patch).toHaveBeenCalledWith(
        "/api/v1/notifications/n1/read",
        {},
      ),
    );
  });
  it("shows count and only allows Sent unread records to be marked", async () => {
    mount(<NotificationsPage />);
    await screen.findByText("Pending notice");
    expect(screen.getAllByRole("button", { name: "Mark read" })).toHaveLength(
      1,
    );
    await userEvent.click(screen.getByRole("button", { name: "Mark read" }));
    await waitFor(() =>
      expect(http.patch).toHaveBeenCalledWith(
        "/api/v1/notifications/n1/read",
        {},
      ),
    );
    expect(canMarkRead({ status: 0 })).toBe(false);
    expect(canMarkRead({ status: 1, readAt: "2026-09-01" })).toBe(false);
    await userEvent.click(
      screen.getByRole("button", { name: "Mark all read" }),
    );
    await waitFor(() =>
      expect(http.patch).toHaveBeenCalledWith(
        "/api/v1/notifications/me/read-all",
        {},
      ),
    );
  });
  it("uses both keyset fields for next and restores the first cursor", async () => {
    pages = true;
    mount(<NotificationsPage />);
    await screen.findByText("Notice 49");
    await userEvent.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() =>
      expect(
        vi
          .mocked(http.get)
          .mock.calls.some(
            (c) =>
              c[0].includes("lastSeenId=n49") &&
              c[0].includes("lastSeenCreatedAt="),
          ),
      ).toBe(true),
    );
    await userEvent.click(screen.getByRole("button", { name: "Previous" }));
    expect(
      screen.getByRole("button", { name: "Previous" }).hasAttribute("disabled"),
    ).toBe(true);
  });
  it("renders loading then empty without invented actions", async () => {
    let resolve!: (v: unknown) => void;
    vi.mocked(http.get).mockImplementation((url) =>
      url.endsWith("/unread-count")
        ? (Promise.resolve(0) as never)
        : (new Promise((r) => {
            resolve = r;
          }) as never),
    );
    mount(<NotificationsPage />);
    expect(screen.getByRole("status", { name: "Loading…" })).toBeTruthy();
    resolve([]);
    await screen.findByText("No records found.");
    expect(screen.queryByRole("button", { name: "Mark read" })).toBeNull();
  });
  it("renders errors with retry", async () => {
    vi.mocked(http.get).mockRejectedValue(new ApiError(503, "Unavailable"));
    mount(<NotificationsPage />);
    await screen.findAllByRole("alert");
    expect(screen.getByRole("button", { name: "Retry" })).toBeTruthy();
  });
});
describe("Settings", () => {
  it("loads company profile and updates only allowed fields", async () => {
    mount(<SettingsPage />);
    await screen.findByDisplayValue("Company Ltd");
    await userEvent.clear(screen.getByLabelText("Legal name"));
    await userEvent.type(screen.getByLabelText("Legal name"), "Updated Ltd");
    fireEvent.submit(screen.getByLabelText("Legal name").closest("form")!);
    await waitFor(() =>
      expect(http.put).toHaveBeenCalledWith("/api/v1/companies/company", {
        legalName: "Updated Ltd",
        displayName: "Aqari",
        primaryPhone: "+962790000000",
        primaryEmail: "owner@example.test",
      }),
    );
  });
  it("validates operational settings and sends None as null", async () => {
    expect(
      settingsSchema.safeParse({ ...settings, lateFeeType: 1, lateFeeValue: 0 })
        .success,
    ).toBe(false);
    expect(
      settingsSchema.safeParse({ ...settings, fiscalYearStartMonth: 13 })
        .success,
    ).toBe(false);
    mount(<SettingsPage />);
    await screen.findByLabelText("Rent grace period (days)");
    fireEvent.change(screen.getByLabelText("Rent grace period (days)"), {
      target: { value: "8" },
    });
    fireEvent.submit(
      screen.getByLabelText("Rent grace period (days)").closest("form")!,
    );
    await waitFor(() =>
      expect(http.put).toHaveBeenCalledWith(
        "/api/v1/companies/company/settings",
        {
          rentGracePeriodDays: 8,
          fiscalYearStartMonth: 1,
          lateFeeType: 0,
          lateFeeValue: null,
        },
      ),
    );
    expect(screen.queryByRole("textbox", { name: "Timezone" })).toBeNull();
  });
  it("keeps company forms read-only without permission", async () => {
    auth.user.permissions = [];
    mount(<SettingsPage />);
    await screen.findByDisplayValue("Company Ltd");
    expect(
      screen.getByLabelText("Legal name").closest("fieldset")?.disabled,
    ).toBe(true);
    expect(screen.queryByRole("button", { name: "Save" })).toBeNull();
  });
  it("shows company loading and failure", async () => {
    vi.mocked(http.get).mockRejectedValue(new ApiError(404, "Missing"));
    mount(<SettingsPage />);
    await screen.findByRole("alert");
    expect(screen.queryByRole("button", { name: "Save" })).toBeNull();
  });
});
