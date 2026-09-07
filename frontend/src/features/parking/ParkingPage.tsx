import React, { useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { apartmentsApi } from "@/features/apartments/api/apartments.api";
import { PageContainer } from "@/shared/components/layout/PageContainer";
import { ArchiveBlockedDialog } from "@/shared/components/ui/ArchiveBlockedDialog";
import { extractArchiveBlockedPayload } from "@/shared/lib/archiveBlocked";
import { useTranslation } from "@/shared/i18n";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Badge } from "@/shared/ui/badge";
import { ContractStatus } from "@/features/leasing/types/leasing.types";
import { leasingApi } from "@/features/leasing/api/leasing.api";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/shared/ui/dialog";
import {
  Table,
  TableHeader,
  TableHead,
  TableRow,
  TableBody,
  TableCell,
} from "@/shared/ui/table";
import {
  BuildingChoice,
  Choice,
  Confirm,
  Empty,
  Failure,
  Field,
  Loading,
  useMvpContext,
} from "../mvp/primitives";
import { parkingApi, parkingSchema, type ParkingSpot } from "./parking.api";
const types = [
  { en: "Standard", ar: "عادي" },
  { en: "Covered", ar: "مسقوف" },
  { en: "Visitor", ar: "زوار" },
  { en: "Disabled access", ar: "موقف لذوي الإعاقة" },
];
const selectableTypes = [0, 3];
const parkingCopy = {
  en: { assignment: "Assignment", actions: "Actions", details: "Details", assigned: "Assigned", unassigned: "Unassigned", assign: "Assign to lease", end: "End assignment", current: "Current assignment", tenant: "Tenant", lease: "Lease contract", starts: "Start date", noCurrent: "This parking spot has no active assignment.", chooseLease: "Active lease contract", hint: "Only active lease contracts in this building are shown.", noLeases: "No eligible active lease contracts are available in this building.", assignSuccess: "Parking spot assigned.", endSuccess: "Parking assignment ended. The historical record is preserved.", endTitle: "End parking assignment?", endDescription: "The spot will become available. This does not delete the assignment history." },
  ar: { assignment: "التخصيص", actions: "الإجراءات", details: "التفاصيل", assigned: "مخصص", unassigned: "غير مخصص", assign: "تخصيص لعقد إيجار", end: "إنهاء التخصيص", current: "التخصيص الحالي", tenant: "المستأجر", lease: "عقد الإيجار", starts: "تاريخ البدء", noCurrent: "لا يوجد تخصيص نشط لهذا الموقف.", chooseLease: "عقد إيجار نشط", hint: "تظهر فقط عقود الإيجار النشطة في هذا المبنى.", noLeases: "لا توجد عقود إيجار نشطة مؤهلة في هذا المبنى.", assignSuccess: "تم تخصيص الموقف.", endSuccess: "تم إنهاء التخصيص مع الاحتفاظ بالسجل التاريخي.", endTitle: "إنهاء تخصيص الموقف؟", endDescription: "سيصبح الموقف متاحاً، ولن يتم حذف سجل التخصيص." },
};
export default function ParkingPage() {
  const { scope } = useMvpContext();
  return <ParkingWorkspace key={scope.join(":")} />;
}
function ParkingWorkspace() {
  const { language } = useTranslation();
  const [building, setBuilding] = useState("");
  return (
    <PageContainer
      showBreadcrumbs={false}
      title={language === "ar" ? "المواقف والكراجات" : "Parking & Garages"}
      description={language === "ar" ? "إدارة المواقف وتخصيصها لعقود الإيجار." : "Manage parking spots and their lease assignments."}
    >
      <BuildingChoice value={building} onChange={setBuilding} />
      {building && <Spots key={building} building={building} />}
    </PageContainer>
  );
}
function Spots({ building }: { building: string }) {
  const { t, language } = useTranslation();
  const p = parkingCopy[language === "ar" ? "ar" : "en"];
  const { scope, can } = useMvpContext();
  const cache = useQueryClient();
  const key = ["parking", ...scope, building];
  const list = useQuery({
    queryKey: key,
    queryFn: () => parkingApi.list(building),
    enabled: can("properties.read"),
    retry: false,
  });
  const [editing, setEditing] = useState<ParkingSpot | "new" | null>(null);
  const [archiving, setArchiving] = useState<ParkingSpot | null>(null);
  const [selected, setSelected] = useState<ParkingSpot | null>(null);
  const archive = useMutation({
    mutationFn: (id: string) => parkingApi.archive(id),
    onSuccess: () => {
      setArchiving(null);
      cache.invalidateQueries({ queryKey: key });
      toast.success(t("mvp.saved"));
    },
  });
  const blocked = extractArchiveBlockedPayload(archive.error);
  if (!can("properties.read")) return <p role="alert">{t("mvp.denied")}</p>;
  return (
    <section className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <Button
          variant="outline"
          onClick={() => list.refetch()}
          disabled={list.isFetching}
        >
          {t("mvp.refresh")}
        </Button>
        {can("properties.create") && (
          <Button onClick={() => setEditing("new")}>{t("mvp.create")}</Button>
        )}
      </div>
      {list.isPending ? (
        <Loading />
      ) : list.error ? (
        <Failure error={list.error} retry={() => list.refetch()} />
      ) : !list.data?.length ? (
        <Empty />
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="text-start">{t("mvp.code")}</TableHead>
              <TableHead className="text-start">{t("mvp.type")}</TableHead>
              <TableHead className="text-start">{t("mvp.location")}</TableHead>
              <TableHead className="text-start">{p.assignment}</TableHead>
              <TableHead className="text-start">{p.actions}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {list.data.map((spot) => (
              <TableRow key={spot.id}>
                <TableCell>
                  <bdi>{spot.spotCode}</bdi>
                </TableCell>
                <TableCell>
                  {types[spot.parkingType]?.[language === "ar" ? "ar" : "en"] ?? "—"}
                </TableCell>
                <TableCell className="max-w-xs whitespace-normal break-words">
                  {spot.locationDescription || "—"}
                </TableCell>
                <TableCell>
                  <Button variant="ghost" onClick={() => setSelected(spot)}>{p.details}</Button>
                </TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    {can("properties.update") && (
                      <Button
                        variant="outline"
                        onClick={() => setEditing(spot)}
                      >
                        {t("mvp.edit")}
                      </Button>
                    )}
                    {can("properties.delete") && (
                      <Button
                        variant="ghost"
                        onClick={() => {
                          archive.reset();
                          setArchiving(spot);
                        }}
                      >
                        {t("mvp.archive")}
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
      {selected && <AssignmentDialog spot={selected} onClose={() => setSelected(null)} />}
      {editing && (
        <ParkingForm
          building={building}
          spot={editing === "new" ? undefined : editing}
          spots={list.data ?? []}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            cache.invalidateQueries({ queryKey: key });
          }}
        />
      )}
      {archiving && !blocked && (
        <Confirm
          title={`${t("mvp.archiveConfirm")} ${archiving.spotCode}`}
          description={t("mvp.archiveNote")}
          pending={archive.isPending}
          error={archive.error}
          onClose={() => setArchiving(null)}
          onConfirm={() =>
            can("properties.delete") && archive.mutate(archiving.id)
          }
        />
      )}
      <ArchiveBlockedDialog
        open={!!blocked}
        data={blocked}
        onOpenChange={() => {
          archive.reset();
          setArchiving(null);
        }}
      />
    </section>
  );
}
function ParkingForm({
  building,
  spot,
  spots,
  onClose,
  onSaved,
}: {
  building: string;
  spot?: ParkingSpot;
  spots: ParkingSpot[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t, direction, language } = useTranslation();
  const { scope, can } = useMvpContext();
  const [code, setCode] = useState(spot?.spotCode ?? suggestedSpotCode(spots));
  const [type, setType] = useState(String(spot?.parkingType ?? 0));
  const [location, setLocation] = useState(spot?.locationDescription ?? "");
  const [apartment, setApartment] = useState(spot?.defaultApartmentId ?? "");
  const [invalid, setInvalid] = useState(false);
  const apartments = useQuery({
    queryKey: ["parking-apartments", ...scope, building],
    queryFn: () => apartmentsApi.getApartments({ buildingId: building }),
    retry: false,
  });
  const mutation = useMutation({
    mutationFn: () => {
      const body = parkingSchema.parse({
        spotCode: code,
        parkingType: Number(type),
        locationDescription: location || null,
        defaultApartmentId: apartment || null,
      });
      return spot
        ? parkingApi.update(spot.id, body)
        : parkingApi.create(building, body);
    },
    onSuccess: () => {
      toast.success(t("mvp.saved"));
      onSaved();
    },
  });
  const allowed = can(spot ? "properties.update" : "properties.create");
  return (
    <Dialog
      open
      onOpenChange={(open) => !open && !mutation.isPending && onClose()}
    >
      <DialogContent dir={direction}>
        <DialogHeader>
          <DialogTitle>{t(spot ? "mvp.edit" : "mvp.create")}</DialogTitle>
          <DialogDescription>{t("mvp.spots")}</DialogDescription>
        </DialogHeader>
        <form
          className="space-y-4"
          onSubmit={(e) => {
            e.preventDefault();
            if (!allowed || mutation.isPending) return;
            const valid = parkingSchema.safeParse({
              spotCode: code,
              parkingType: Number(type),
              locationDescription: location || null,
              defaultApartmentId: apartment || null,
            }).success;
            setInvalid(!valid);
            if (valid) mutation.mutate();
          }}
        >
          <Field label={t("mvp.code")}>
            <Input
              value={code}
              onChange={(e) => setCode(e.target.value)}
              required
              maxLength={50}
            />
          </Field>
          <Field label={t("mvp.type")}>
            <Choice
              value={type}
              onChange={setType}
              options={selectableTypes.map((value) => ({
                value: String(value),
                label: types[value][language === "ar" ? "ar" : "en"],
              }))}
            />
          </Field>
          {spot && !selectableTypes.includes(spot.parkingType) && (
            <p className="text-xs text-muted-foreground">
              {language === "ar" ? `النوع المحفوظ: ${types[spot.parkingType]?.ar ?? "—"}` : `Stored type: ${types[spot.parkingType]?.en ?? "—"}`}
            </p>
          )}
          <Field label={t("mvp.location")}>
            <Input
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              maxLength={255}
            />
          </Field>
          {apartments.isPending ? (
            <Loading />
          ) : apartments.error ? (
            <Failure
              error={apartments.error}
              retry={() => apartments.refetch()}
            />
          ) : (
            <Field label={t("mvp.apartment")}>
              <Choice
                value={apartment}
                onChange={setApartment}
                options={[
                  { value: "", label: t("mvp.none") },
                  ...(apartments.data ?? []).map((a) => ({
                    value: a.id,
                    label: a.unitNumber,
                  })),
                ]}
              />
            </Field>
          )}
          {invalid && <p role="alert">{t("mvp.required")}</p>}
          {mutation.error && <Failure error={mutation.error} />}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={onClose}
            >
              {t("mvp.cancel")}
            </Button>
            <Button
              type="submit"
              loading={mutation.isPending}
              disabled={!allowed || apartments.isPending || !!apartments.error}
            >
              {t("mvp.save")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function suggestedSpotCode(spots: ParkingSpot[]): string {
  const used = new Set(spots.map((spot) => spot.spotCode.trim().toUpperCase()));
  for (let number = 1; ; number += 1) {
    const suggestion = `P-${String(number).padStart(2, "0")}`;
    if (!used.has(suggestion)) return suggestion;
  }
}

function AssignmentDialog({ spot, onClose }: { spot: ParkingSpot; onClose: () => void }) {
  const { language, direction } = useTranslation();
  const navigate = useNavigate();
  const p = parkingCopy[language === "ar" ? "ar" : "en"];
  const { can } = useMvpContext();
  const cache = useQueryClient();
  const [leaseId, setLeaseId] = useState("");
  const [leaseSearch, setLeaseSearch] = useState("");
  const [confirmEnd, setConfirmEnd] = useState(false);
  const assignmentKey = ["parking-assignment", spot.id];
  const current = useQuery({
    queryKey: assignmentKey,
    queryFn: () => parkingApi.getCurrentAssignment(spot.id),
    retry: false,
  });
  const leases = useQuery({
    queryKey: ["leases", "parking-eligible", spot.buildingId],
    queryFn: () => leasingApi.searchContracts("", 100),
    enabled: current.isSuccess && !current.data && can("properties.update"),
    retry: false,
  });
  const tenants = useQuery({
    queryKey: ["leases", "parking-tenants"],
    queryFn: () => leasingApi.getTenants(""),
    enabled: current.isSuccess && !current.data && can("properties.update"),
    retry: false,
  });
  const currentLease = useQuery({
    queryKey: ["leases", "detail", current.data?.leaseContractId],
    queryFn: () => leasingApi.getContractById(current.data!.leaseContractId),
    enabled: !!current.data?.leaseContractId,
    retry: false,
  });
  const eligible = useMemo(() => (leases.data ?? [])
    .filter((lease) => lease.status === ContractStatus.Active && lease.buildingId === spot.buildingId)
    .map((lease) => ({ ...lease, tenantName: tenants.data?.find((tenant) => tenant.id === lease.tenantId)?.name }))
    .filter((lease) => {
      const search = leaseSearch.trim().toLocaleLowerCase();
      return !search || lease.contractNumber.toLocaleLowerCase().includes(search) || lease.tenantName?.toLocaleLowerCase().includes(search);
    }), [leases.data, tenants.data, spot.buildingId, leaseSearch]);
  const refresh = async (contractId?: string) => {
    await cache.invalidateQueries({ queryKey: assignmentKey });
    await cache.invalidateQueries({ queryKey: ["parking"] });
    if (contractId) await cache.invalidateQueries({ queryKey: ["lease-parking", contractId] });
  };
  const assign = useMutation({
    mutationFn: () => parkingApi.assign(spot.id, leaseId),
    onSuccess: async () => {
      await refresh(leaseId);
      setLeaseId("");
      toast.success(p.assignSuccess);
    },
  });
  const end = useMutation({
    mutationFn: () => parkingApi.endAssignment(current.data!.assignmentId),
    onSuccess: async () => {
      const contractId = current.data?.leaseContractId;
      setConfirmEnd(false);
      await refresh(contractId);
      toast.success(p.endSuccess);
    },
  });
  return (
    <>
      <Dialog open onOpenChange={(open) => !open && !assign.isPending && !end.isPending && onClose()}>
        <DialogContent dir={direction}>
          <DialogHeader>
            <DialogTitle>{p.current}: <bdi>{spot.spotCode}</bdi></DialogTitle>
            <DialogDescription>{spot.locationDescription || types[spot.parkingType]?.[language === "ar" ? "ar" : "en"]}</DialogDescription>
          </DialogHeader>
          {current.isPending ? <Loading /> : current.error ? <Failure error={current.error} retry={() => current.refetch()} /> : current.data ? (
            <div className="space-y-4">
              <Badge>{p.assigned}</Badge>
              <dl className="grid gap-3 rounded-lg border p-4 sm:grid-cols-2">
                <div><dt className="text-xs text-muted-foreground">{p.tenant}</dt><dd className="font-medium">{current.data.tenantName}</dd></div>
                <div><dt className="text-xs text-muted-foreground">{p.lease}</dt><dd>{currentLease.isPending ? <span className="text-sm text-muted-foreground">…</span> : currentLease.data ? <Button variant="link" className="h-auto p-0 font-medium" onClick={() => navigate(`/leases/${currentLease.data!.id}`)}><bdi>{currentLease.data.contractNumber}</bdi></Button> : <span className="text-sm text-muted-foreground">—</span>}</dd></div>
                <div><dt className="text-xs text-muted-foreground">{p.starts}</dt><dd><bdi>{current.data.startDate}</bdi></dd></div>
              </dl>
              {end.error && <Failure error={end.error} />}
              {can("properties.update") && <Button variant="destructive" onClick={() => setConfirmEnd(true)}>{p.end}</Button>}
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-center gap-2"><Badge variant="outline">{p.unassigned}</Badge><span className="text-sm text-muted-foreground">{p.noCurrent}</span></div>
              {can("properties.update") && (leases.isPending || tenants.isPending ? <Loading /> : leases.error || tenants.error ? <Failure error={leases.error ?? tenants.error} retry={() => { leases.refetch(); tenants.refetch(); }} /> : eligible.length ? <>
                <Field label={language === "ar" ? "ابحث برقم العقد أو اسم المستأجر" : "Search by contract or tenant"}><Input value={leaseSearch} onChange={(event) => setLeaseSearch(event.target.value)} /></Field>
                <Field label={language === "ar" ? "اختر عقد الإيجار" : p.chooseLease}><Choice value={leaseId} onChange={setLeaseId} options={[{ value: "", label: language === "ar" ? "اختر عقد الإيجار" : p.chooseLease }, ...eligible.map((lease) => ({ value: lease.id, label: lease.tenantName ? `${lease.contractNumber} — ${lease.tenantName}` : lease.contractNumber }))]} /></Field>
                <p className="text-xs text-muted-foreground">{p.hint}</p>
                {assign.error && <Failure error={assign.error} />}
                <Button disabled={!leaseId} loading={assign.isPending} onClick={() => assign.mutate()}>{p.assign}</Button>
              </> : <p className="text-sm text-muted-foreground">{p.noLeases}</p>)}
            </div>
          )}
        </DialogContent>
      </Dialog>
      {confirmEnd && <Confirm title={p.endTitle} description={p.endDescription} pending={end.isPending} error={end.error} onClose={() => setConfirmEnd(false)} onConfirm={() => end.mutate()} />}
    </>
  );
}
