import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router";
import { toast } from "sonner";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useTranslation } from "@/shared/i18n";
import { useTheme } from "@/shared/theme/ThemeProvider";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import {
  Choice,
  Confirm,
  Failure,
  Field,
  Loading,
  useMvpContext,
} from "../mvp/primitives";
import {
  settingsApi,
  profileSchema,
  settingsSchema,
  type Company,
  type CompanySettings,
} from "./settings.api";
export default function SettingsPage() {
  const { scope } = useMvpContext();
  return <SettingsWorkspace key={scope.join(":")} />;
}
function SettingsWorkspace() {
  const { scope, user } = useMvpContext();
  const { logout } = useAuth();
  const { t, setLanguage, language } = useTranslation();
  const { theme, setTheme } = useTheme();
  const [confirm, setConfirm] = useState(false);
  const company = useQuery({
    queryKey: ["settings-company", ...scope],
    queryFn: settingsApi.company,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    enabled: !!user?.companyId,
    retry: false,
  });
  const all = useMutation({
    mutationFn: settingsApi.logoutAll,
    onSuccess: () => logout(),
  });
  return (
    <PageContainer showBreadcrumbs={false} title={t("mvp.settings")}>
      {!user?.companyId ? (
        <p role="alert">{t("mvp.denied")}</p>
      ) : company.isPending ? (
        <Loading />
      ) : company.error ? (
        <Failure error={company.error} retry={() => company.refetch()} />
      ) : company.data?.id !== user.companyId ? (
        <p role="alert">{t("mvp.denied")}</p>
      ) : (
        <>
          <Profile key={company.dataUpdatedAt} company={company.data} />
          <Operations companyId={company.data.id} />
        </>
      )}
      <section className="space-y-4 border-t pt-6">
        <h2 className="type-title-large">{t("mvp.appearance")}</h2>
        <div className="grid max-w-2xl gap-4 sm:grid-cols-2">
          <Field label={t("mvp.appearance")}>
            <Choice
              value={theme}
              onChange={(v) => setTheme(v as "light" | "dark" | "system")}
              options={["light", "dark", "system"].map((v) => ({
                value: v,
                label: t(`mvp.${v}`),
              }))}
            />
          </Field>
          <Field label={t("mvp.language")}>
            <Choice
              value={language}
              onChange={(v) => setLanguage(v as "ar" | "en")}
              options={[
                { value: "ar", label: "العربية" },
                { value: "en", label: "English" },
              ]}
            />
          </Field>
        </div>
      </section>
      <section className="space-y-4 border-t pt-6">
        <h2 className="type-title-large">{t("mvp.profile")}</h2>
        <p className="break-words">
          {user?.name} · <bdi>{user?.email}</bdi>
        </p>
        <div className="flex flex-wrap gap-3">
          <Button variant="outline" onClick={() => logout()}>
            {t("mvp.logout")}
          </Button>
          <Button
            variant="outline"
            onClick={() => {
              all.reset();
              setConfirm(true);
            }}
          >
            {t("mvp.logoutAll")}
          </Button>
          <Link
            className={buttonVariants({ variant: "link" })}
            to="/auth/forgot-password"
          >
            {t("mvp.recovery")}
          </Link>
          <Link
            className={buttonVariants({ variant: "link" })}
            to="/subscriptions"
          >
            {t("mvp.subscription")}
          </Link>
        </div>
      </section>
      {confirm && (
        <Confirm
          title={t("mvp.logoutAll")}
          description={t("mvp.logoutAllNote")}
          pending={all.isPending}
          error={all.error}
          onClose={() => setConfirm(false)}
          onConfirm={() => all.mutate()}
        />
      )}
    </PageContainer>
  );
}
function Profile({ company }: { company: Company }) {
  const { can, scope } = useMvpContext();
  const { t } = useTranslation();
  const cache = useQueryClient();
  const [values, setValues] = useState({
    legalName: company.legalName,
    displayName: company.displayName,
    primaryPhone: company.primaryPhone,
    primaryEmail: company.primaryEmail ?? "",
  });
  const [invalid, setInvalid] = useState(false);
  const save = useMutation({
    mutationFn: () =>
      settingsApi.updateProfile(
        company.id,
        profileSchema.parse({
          ...values,
          primaryEmail: values.primaryEmail || null,
        }),
      ),
    onSuccess: () => {
      cache.invalidateQueries({ queryKey: ["settings-company", ...scope] });
      toast.success(t("mvp.saved"));
    },
  });
  const fields = [
    ["legalName", "legalName"],
    ["displayName", "displayName"],
    ["primaryPhone", "phone"],
    ["primaryEmail", "email"],
  ] as const;
  return (
    <section className="space-y-4">
      <h2 className="type-title-large">{t("mvp.companyProfile")}</h2>
      <dl className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {[
          ["registration", company.commercialRegistrationNo],
          ["tax", company.taxNumber],
          [
            "companyType",
            t(
              `mvp.${["individualOwner", "managementCompany", "investmentCompany"][company.companyType] ?? "none"}`,
            ),
          ],
          ["country", company.countryCode],
          ["active", t(company.isActive ? "mvp.active" : "mvp.inactive")],
        ].map(([k, v]) => (
          <div key={k}>
            <dt className="text-sm text-muted-foreground">{t(`mvp.${k}`)}</dt>
            <dd className="break-words">
              <bdi>{v || "—"}</bdi>
            </dd>
          </div>
        ))}
      </dl>
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (!can("company.manage") || save.isPending) return;
          const valid = profileSchema.safeParse(values).success;
          setInvalid(!valid);
          if (valid) save.mutate();
        }}
      >
        <fieldset
          disabled={!can("company.manage") || save.isPending}
          className="grid gap-4 sm:grid-cols-2"
        >
          {fields.map(([key, label]) => (
            <Field key={key} label={t(`mvp.${label}`)}>
              <Input
                value={values[key]}
                type={key === "primaryEmail" ? "email" : "text"}
                required={key !== "primaryEmail"}
                onChange={(e) =>
                  setValues((v) => ({ ...v, [key]: e.target.value }))
                }
              />
            </Field>
          ))}
        </fieldset>
        {invalid && <p role="alert">{t("mvp.required")}</p>}
        {save.error && <Failure error={save.error} />}{" "}
        {can("company.manage") && (
          <Button type="submit" loading={save.isPending}>
            {t("mvp.save")}
          </Button>
        )}
      </form>
    </section>
  );
}
function Operations({ companyId }: { companyId: string }) {
  const { scope } = useMvpContext();
  const query = useQuery({
    queryKey: ["company-operations", ...scope, companyId],
    queryFn: () => settingsApi.settings(companyId),
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    retry: false,
  });
  return query.isPending ? (
    <Loading />
  ) : query.error ? (
    <Failure error={query.error} retry={() => query.refetch()} />
  ) : (
    <OperationsForm
      key={query.dataUpdatedAt}
      companyId={companyId}
      settings={query.data!}
    />
  );
}
function OperationsForm({
  companyId,
  settings,
}: {
  companyId: string;
  settings: CompanySettings;
}) {
  const { t } = useTranslation();
  const { can, scope } = useMvpContext();
  const cache = useQueryClient();
  const [grace, setGrace] = useState(String(settings.rentGracePeriodDays));
  const [month, setMonth] = useState(String(settings.fiscalYearStartMonth));
  const [type, setType] = useState(String(settings.lateFeeType));
  const [value, setValue] = useState(String(settings.lateFeeValue ?? ""));
  const [invalid, setInvalid] = useState(false);
  const body = () => ({
    rentGracePeriodDays: Number(grace),
    fiscalYearStartMonth: Number(month),
    lateFeeType: Number(type),
    lateFeeValue: type === "0" ? null : Number(value),
  });
  const save = useMutation({
    mutationFn: () =>
      settingsApi.updateSettings(companyId, settingsSchema.parse(body())),
    onSuccess: () => {
      cache.invalidateQueries({
        queryKey: ["company-operations", ...scope, companyId],
      });
      toast.success(t("mvp.saved"));
    },
  });
  return (
    <section className="space-y-4 border-t pt-6">
      <h2 className="type-title-large">{t("mvp.operations")}</h2>
      <dl className="grid gap-3 sm:grid-cols-3">
        {[
          ["currency", settings.defaultCurrency],
          ["language", settings.defaultLanguage],
          ["timezone", settings.timezone],
        ].map(([k, v]) => (
          <div key={k}>
            <dt className="text-sm text-muted-foreground">{t(`mvp.${k}`)}</dt>
            <dd>
              <bdi>{v}</bdi>
            </dd>
          </div>
        ))}
      </dl>
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (!can("company.manage") || save.isPending) return;
          const valid =
            !!grace && !!month && settingsSchema.safeParse(body()).success;
          setInvalid(!valid);
          if (valid) save.mutate();
        }}
      >
        <fieldset
          disabled={!can("company.manage") || save.isPending}
          className="grid gap-4 sm:grid-cols-2"
        >
          <Field label={t("mvp.grace")}>
            <Input
              type="number"
              required
              min={0}
              max={32767}
              step={1}
              value={grace}
              onChange={(e) => setGrace(e.target.value)}
            />
          </Field>
          <Field label={t("mvp.fiscal")}>
            <Input
              type="number"
              required
              min={1}
              max={12}
              step={1}
              value={month}
              onChange={(e) => setMonth(e.target.value)}
            />
          </Field>
          <Field label={t("mvp.feeType")}>
            <Choice
              value={type}
              onChange={setType}
              disabled={!can("company.manage") || save.isPending}
              options={["none", "fixed", "percentage"].map((v, i) => ({
                value: String(i),
                label: t(`mvp.${v}`),
              }))}
            />
          </Field>
          {type !== "0" && (
            <Field label={t("mvp.feeValue")}>
              <Input
                type="number"
                required
                min="0.001"
                step="0.001"
                value={value}
                onChange={(e) => setValue(e.target.value)}
              />
            </Field>
          )}
        </fieldset>
        {invalid && <p role="alert">{t("mvp.required")}</p>}
        {save.error && <Failure error={save.error} />}{" "}
        {can("company.manage") && (
          <Button type="submit" loading={save.isPending}>
            {t("mvp.save")}
          </Button>
        )}
      </form>
    </section>
  );
}
