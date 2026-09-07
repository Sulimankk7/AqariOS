import assert from "node:assert/strict";
import { createServer } from "vite";

const vite = await createServer({ server: { middlewareMode: true }, appType: "custom" });

try {
  const { ApiError } = await vite.ssrLoadModule("/src/shared/lib/http.ts");
  const { extractUserFriendlyError, getUserFacingErrorKind, mapApiValidationErrors } =
    await vite.ssrLoadModule("/src/shared/utils/errorHandling.ts");
  const { setRuntimeLanguage } = await vite.ssrLoadModule("/src/shared/i18n/runtime.ts");
  const { getFloorTranslation } = await vite.ssrLoadModule("/src/features/floors/constants/translations.ts");

  setRuntimeLanguage("en");
  const conflict = new ApiError(409, "A floor with number 3 already exists in this building.", {
    title: "Resource Conflict",
    detail: "A floor with number 3 already exists in this building.",
  });
  assert.equal(extractUserFriendlyError(conflict), conflict.detail);
  assert.equal(getUserFacingErrorKind(conflict), "conflict");
  assert.equal(
    getFloorTranslation("numberExists", "en").replace("{number}", "3"),
    "Floor number 3 already exists in this building.",
  );
  assert.equal(
    getFloorTranslation("numberExists", "ar").replace("{number}", "3"),
    "رقم الطابق 3 موجود مسبقًا في هذا المبنى.",
  );

  const conflictWithoutDetail = new ApiError(409, "HTTP 409: Conflict", { title: "Resource Conflict" });
  assert.equal(extractUserFriendlyError(conflictWithoutDetail), "The data conflicts with a recent change. Refresh and try again.");

  const codedConflict = new ApiError(409, "Conflict", { code: "EMAIL_ALREADY_EXISTS" });
  assert.equal(extractUserFriendlyError(codedConflict), "This email address is already in use.");

  const technical500 = new ApiError(500, "SQLSTATE 23505 at DbContext.SaveChanges", {
    detail: "SQLSTATE 23505 at DbContext.SaveChanges",
  });
  assert.equal(extractUserFriendlyError(technical500), "A server error occurred. Please try again later.");

  const validation = new ApiError(400, "Validation failed", {
    errors: { UnitNumber: ["Unit number is required."] },
  });
  const mapped = [];
  assert.equal(mapApiValidationErrors(validation, (field, details) => mapped.push([field, details.message])), true);
  assert.deepEqual(mapped, [["unitNumber", "This field is required"]]);

  assert.equal(getUserFacingErrorKind(new ApiError(401, "Unauthorized")), "unauthorized");
  assert.equal(getUserFacingErrorKind(new ApiError(403, "Forbidden")), "forbidden");
  assert.equal(getUserFacingErrorKind(new ApiError(404, "Not found")), "notFound");
  assert.equal(getUserFacingErrorKind(new ApiError(429, "Too many requests")), "rateLimited");
  assert.equal(getUserFacingErrorKind(new ApiError(503, "Unavailable")), "server");
  assert.equal(getUserFacingErrorKind(new ApiError(504, "Timeout")), "timeout");
  assert.equal(getUserFacingErrorKind(new ApiError(500, "Failed to fetch")), "network");

  assert.equal(extractUserFriendlyError(new Error("NetworkError when attempting to fetch resource")), "Network connection failed. Check your internet connection.");
  assert.equal(extractUserFriendlyError(new Error("Load failed")), "Network connection failed. Check your internet connection.");
  assert.equal(extractUserFriendlyError(new ApiError(504, "Gateway Timeout")), "The request timed out. Please try again.");

  setRuntimeLanguage("ar");
  assert.equal(extractUserFriendlyError(codedConflict), "عنوان البريد الإلكتروني مستخدم بالفعل.");
  const unmappedCodedError = new ApiError(422, "English business rule", {
    code: "BACKEND_CODE_NOT_YET_MAPPED",
    detail: "This English detail must not leak into Arabic UI.",
  });
  assert.equal(extractUserFriendlyError(unmappedCodedError, "تعذر تنفيذ الإجراء المطلوب."), "تعذر تنفيذ الإجراء المطلوب.");
  assert.equal(extractUserFriendlyError(conflictWithoutDetail), "تتعارض البيانات مع تغيير حديث. حدّث الصفحة وحاول مجدداً.");
  assert.equal(extractUserFriendlyError(new ApiError(429, "Too many requests", undefined, 37)), "تم تجاوز عدد الطلبات المسموح. يرجى المحاولة مرة أخرى بعد 37 ثانية.");
  assert.equal(extractUserFriendlyError(new Error("Failed to fetch")), "فشل الاتصال بالشبكة. تحقق من اتصال الإنترنت.");

  const arabicMapped = [];
  assert.equal(mapApiValidationErrors(validation, (field, details) => arabicMapped.push([field, details.message])), true);
  assert.deepEqual(arabicMapped, [["unitNumber", "حقل مطلوب"]]);

  // Safe, non-technical backend detail without a stable code remains backend-owned by design.
  assert.equal(extractUserFriendlyError(conflict), conflict.detail);

  setRuntimeLanguage("en");
  assert.equal(
    extractUserFriendlyError(new Error("Unexpected failure"), "تعذر تنفيذ الإجراء."),
    "Unable to complete the operation. Please try again.",
  );

  console.log("Error-handling verification passed (runtime language switching, codes, safe detail, validation, auth, rate limit, 5xx, timeout, browser/network)." );
} finally {
  await vite.close();
}
