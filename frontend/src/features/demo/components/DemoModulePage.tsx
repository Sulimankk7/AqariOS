import { Plus } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { DataTable, type Column } from "@/shared/components/ui/DataTable";
import { PageHeader } from "@/shared/components/ui/Headers";
import { DEMO_MODULES, type DemoModuleKey, type DemoTableRow } from "../data/demoData";

export interface DemoModulePageProps {
  moduleKey: DemoModuleKey;
}

function statusVariant(status: string): "tonal" | "outline" | "secondary" {
  if (["نشط", "مدفوعة", "دخل", "ساري", "جديد", "مشغولة"].includes(status)) {
    return "tonal";
  }
  if (["متأخرة", "بحاجة للمتابعة", "بحاجة للتجديد"].includes(status)) {
    return "secondary";
  }
  return "outline";
}

export function DemoModulePage({ moduleKey }: DemoModulePageProps) {
  const module = DEMO_MODULES[moduleKey];
  const columns: Column<DemoTableRow>[] = [
    { key: "primary", header: module.columns.primary, accessor: (row) => row.primary, sortable: true, cell: (row) => <strong className="font-semibold text-foreground">{row.primary}</strong> },
    { key: "secondary", header: module.columns.secondary, accessor: (row) => row.secondary, sortable: true },
    { key: "relation", header: module.columns.relation, accessor: (row) => row.relation },
    { key: "status", header: module.columns.status, accessor: (row) => row.status, sortable: true, cell: (row) => <Badge variant={statusVariant(row.status)}>{row.status}</Badge> },
    { key: "value", header: module.columns.value, accessor: (row) => row.value, sortable: true },
  ].filter((column) => column.header);

  const explainReadOnly = () => {
    toast.info("وضع التجربة للقراءة فقط. لن يتم إرسال أو حفظ أي تغييرات.");
  };

  return (
    <section data-demo-module={moduleKey} className="space-y-6">
      <PageHeader
        title={module.title}
        description={module.description}
        badge={<Badge variant="outline">بيانات تجريبية</Badge>}
        actions={module.actionLabel ? (
          <Button type="button" onClick={explainReadOnly}>
            <Plus aria-hidden="true" />
            {module.actionLabel}
          </Button>
        ) : undefined}
      />

      <DataTable
        data={module.rows}
        columns={columns}
        searchable
        searchPlaceholder={`ابحث في ${module.title}`}
        pageSize={8}
        onRowClick={explainReadOnly}
      />
    </section>
  );
}
