import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformAdministratorsPage } from "./PlatformAdministratorsPage";
import { ApiError } from "@/shared/lib/http";

const refetch = vi.fn().mockResolvedValue(undefined);
const mutateAsync = vi.fn().mockResolvedValue(undefined);
let createPending = false;

vi.mock("../hooks/usePlatformAdministrators", () => ({
  usePlatformAdministrators: () => ({
    data: [{ id: "admin-1", fullName: "Existing Admin", email: "admin@example.com", isActive: true, createdAt: "2026-09-10T00:00:00Z" }],
    isLoading: false,
    isError: false,
    isFetching: false,
    error: null,
    refetch,
  }),
  useCreatePlatformAdministrator: () => ({ mutateAsync, isPending: createPending }),
}));

vi.mock("@/shared/i18n", () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    formatDate: (value: string) => value,
  }),
}));

vi.mock("sonner", () => ({ toast: { success: vi.fn() } }));

describe("PlatformAdministratorsPage", () => {
  beforeEach(() => {
    refetch.mockClear();
    mutateAsync.mockClear();
    mutateAsync.mockResolvedValue(undefined);
    createPending = false;
  });

  it("renders the administrator list without sensitive fields", () => {
    render(<PlatformAdministratorsPage />);

    expect(screen.getByText("Existing Admin")).toBeTruthy();
    expect(screen.getByText("admin@example.com")).toBeTruthy();
    expect(screen.queryByLabelText("platformAdmin.administrators.password")).toBeNull();
  });

  it("exposes only name email and password and refreshes after creation", async () => {
    const user = userEvent.setup();
    render(<PlatformAdministratorsPage />);

    await user.click(screen.getByRole("button", { name: /platformAdmin\.administrators\.add/ }));

    const name = screen.getByLabelText("platformAdmin.administrators.fullName");
    const email = screen.getByLabelText("platformAdmin.administrators.email");
    const password = screen.getByLabelText("platformAdmin.administrators.password");
    expect(password.getAttribute("type")).toBe("password");
    expect(screen.getAllByRole("textbox")).toHaveLength(2);

    await user.type(name, "New Admin");
    await user.type(email, "new@example.com");
    await user.type(password, "password123");
    await user.click(screen.getByRole("button", { name: "platformAdmin.administrators.create" }));

    await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({
      fullName: "New Admin",
      email: "new@example.com",
      password: "password123",
    }));
    await waitFor(() => expect(refetch).toHaveBeenCalled());
    expect(screen.queryByLabelText("platformAdmin.administrators.password")).toBeNull();
  });

  it("shows duplicate email conflicts in the form", async () => {
    mutateAsync.mockRejectedValueOnce(new ApiError(409, "Conflict", { code: "EMAIL_ALREADY_EXISTS" }));
    const user = userEvent.setup();
    render(<PlatformAdministratorsPage />);

    await user.click(screen.getByRole("button", { name: /platformAdmin\.administrators\.add/ }));
    await user.type(screen.getByLabelText("platformAdmin.administrators.fullName"), "Duplicate Admin");
    await user.type(screen.getByLabelText("platformAdmin.administrators.email"), "admin@example.com");
    await user.type(screen.getByLabelText("platformAdmin.administrators.password"), "password123");
    await user.click(screen.getByRole("button", { name: "platformAdmin.administrators.create" }));

    expect(await screen.findByText("platformAdmin.administrators.duplicateEmail")).toBeTruthy();
    expect(refetch).not.toHaveBeenCalled();
  });

  it("disables the form while creation is pending", async () => {
    const user = userEvent.setup();
    const view = render(<PlatformAdministratorsPage />);
    await user.click(screen.getByRole("button", { name: /platformAdmin\.administrators\.add/ }));

    createPending = true;
    view.rerender(<PlatformAdministratorsPage />);

    expect((screen.getByLabelText("platformAdmin.administrators.fullName") as HTMLInputElement).disabled).toBe(true);
    expect((screen.getByRole("button", { name: "common.processing" }) as HTMLButtonElement).disabled).toBe(true);
  });
});
