import { useQuery } from "@tanstack/react-query";
import { Car } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/app/components/ui/card";
import { Badge } from "@/app/components/ui/badge";
import { Button } from "@/app/components/ui/button";
import { useTranslation } from "@/shared/i18n";
import { parkingApi } from "./parking.api";

export function LeaseParkingSection({ leaseContractId }: { leaseContractId: string }) {
  const { language } = useTranslation();
  const ar = language === "ar";
  const query = useQuery({
    queryKey: ["lease-parking", leaseContractId],
    queryFn: () => parkingApi.getLeaseParking(leaseContractId),
    retry: false,
  });
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-lg"><Car className="h-5 w-5 text-primary" />{ar ? "مواقف عقد الإيجار" : "Lease parking"}</CardTitle>
        <CardDescription>{ar ? "المواقف المخصصة حالياً لهذا العقد" : "Parking spots currently assigned to this contract"}</CardDescription>
      </CardHeader>
      <CardContent>
        {query.isPending ? <p role="status" className="text-sm text-muted-foreground">{ar ? "جارٍ التحميل…" : "Loading…"}</p> : query.error ? <div className="flex items-center justify-between gap-3"><p role="alert" className="text-sm text-destructive">{ar ? "تعذر تحميل المواقف." : "Unable to load parking."}</p><Button size="sm" variant="outline" onClick={() => query.refetch()}>{ar ? "إعادة المحاولة" : "Retry"}</Button></div> : !query.data?.length ? <p className="text-sm text-muted-foreground">{ar ? "لا توجد مواقف مخصصة لهذا العقد." : "No parking spots are assigned to this contract."}</p> : <div className="grid gap-3 sm:grid-cols-2">{query.data.map((item) => <div key={item.assignmentId} className="rounded-lg border p-4"><div className="flex items-center justify-between gap-3"><bdi className="font-semibold">{item.parkingSpotCode}</bdi><Badge>{ar ? "مخصص" : "Assigned"}</Badge></div>{item.location && <p className="mt-2 text-sm text-muted-foreground">{item.location}</p>}<p className="mt-2 text-xs text-muted-foreground">{ar ? "منذ" : "Since"} <bdi>{item.startDate}</bdi></p></div>)}</div>}
      </CardContent>
    </Card>
  );
}
