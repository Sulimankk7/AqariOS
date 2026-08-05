import React from 'react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/app/components/ui/alert-dialog';
import { Card } from '@/app/components/ui/card';
import { Badge } from '@/app/components/ui/badge';
import { Layers, Home, FileText, Wrench, Store, Car, AlertTriangle } from 'lucide-react';
import { ArchiveBlockedPayload } from '@/shared/lib/archiveBlocked';
import { useTranslation } from '@/shared/i18n';

interface ArchiveBlockedDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  data: ArchiveBlockedPayload | null;
}

const DEPENDENCY_META: Record<
  string,
  { en: string; ar: string; icon: React.ComponentType<{ className?: string }> }
> = {
  FLOORS: { en: 'Floors', ar: 'الطوابق', icon: Layers },
  APARTMENTS: { en: 'Apartments', ar: 'الشقق والوحدات', icon: Home },
  LEASE_CONTRACTS: { en: 'Active Lease Contracts', ar: 'عقود الإيجار النشطة', icon: FileText },
  MAINTENANCE_REQUESTS: { en: 'Open Maintenance Requests', ar: 'طلبات الصيانة المفتوحة', icon: Wrench },
  MARKETPLACE_LISTINGS: { en: 'Marketplace Listings', ar: 'إعلانات السوق العقاري', icon: Store },
  PARKING_SPOTS: { en: 'Parking Spots', ar: 'مواقف السيارات', icon: Car },
  PARKING_ASSIGNMENTS: { en: 'Parking Assignments', ar: 'تخصيصات المواقف', icon: Car },
};

export function ArchiveBlockedDialog({ open, onOpenChange, data }: ArchiveBlockedDialogProps) {
  const { language, direction } = useTranslation();
  const isAr = language === 'ar';

  if (!data) return null;

  // Format header title and description cleanly for Arabic and English
  let entityTypeEn = 'item';
  let entityTypeAr = 'هذا العنصر';

  let title = data.title || (isAr ? 'تعذر أرشفة السجل' : 'Cannot be archived');
  if (data.title) {
    if (data.title.includes('Building')) {
      entityTypeEn = 'building';
      entityTypeAr = 'المبنى';
      const match = data.title.match(/Building "(.*?)" cannot be archived/i);
      if (isAr) {
        title = match ? `لا يمكن أرشفة المبنى "${match[1]}"` : 'لا يمكن أرشفة هذا المبنى';
      }
    } else if (data.title.includes('Floor')) {
      entityTypeEn = 'floor';
      entityTypeAr = 'الطابق';
      const match = data.title.match(/Floor "(.*?)" cannot be archived/i);
      if (isAr) {
        title = match ? `لا يمكن أرشفة الطابق "${match[1]}"` : 'لا يمكن أرشفة هذا الطابق';
      }
    } else if (data.title.includes('Apartment')) {
      entityTypeEn = 'apartment';
      entityTypeAr = 'الشقة';
      const match = data.title.match(/Apartment "(.*?)" cannot be archived/i);
      if (isAr) {
        title = match ? `لا يمكن أرشفة الشقة "${match[1]}"` : 'لا يمكن أرشفة هذه الشقة';
      }
    } else if (data.title.includes('ParkingSpot')) {
      entityTypeEn = 'parking spot';
      entityTypeAr = 'موقف السيارات';
      const match = data.title.match(/ParkingSpot "(.*?)" cannot be archived/i);
      if (isAr) {
        title = match ? `لا يمكن أرشفة موقف السيارات "${match[1]}"` : 'لا يمكن أرشفة هذا الموقف';
      }
    }
  }

  const shortDescription = isAr
    ? `يحتوي ${entityTypeAr} على سجلات نشطة. يرجى أرشفة أو إزالة العناصر التالية أولاً.`
    : `This ${entityTypeEn} still contains active records. Archive or remove the following items first.`;

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent dir={direction} className="max-w-md sm:max-w-lg border-border shadow-xl">
        <AlertDialogHeader className="text-start">
          <div className="flex items-start gap-3 mb-1">
            <div className="p-2.5 rounded-lg bg-destructive/10 text-destructive border border-destructive/20 shrink-0 mt-0.5">
              <AlertTriangle className="h-5 w-5" />
            </div>
            <div className="space-y-1">
              <AlertDialogTitle className="text-lg font-semibold text-foreground leading-snug" dir="auto">
                {title}
              </AlertDialogTitle>
              <AlertDialogDescription className="text-muted-foreground text-sm leading-relaxed" dir="auto">
                {shortDescription}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        {/* Body: Dependency cards using the application's existing Card component */}
        <div className="my-2 max-h-[55vh] overflow-y-auto space-y-3 pr-1 pl-1">
          {data.dependencies.map((dep) => {
            const meta = DEPENDENCY_META[dep.code];
            const IconComponent = meta?.icon || Layers;
            const labelText = isAr ? (meta?.ar || dep.label) : (meta?.en || dep.label);

            const hasExamples = dep.examples && dep.examples.length > 0;
            const remainingCount = hasExamples && dep.count > dep.examples!.length
              ? dep.count - dep.examples!.length
              : 0;

            return (
              <Card
                key={dep.code}
                className="p-4 flex-col gap-3 rounded-lg border-border bg-card text-card-foreground shadow-2xs transition-colors hover:border-muted-foreground/30"
              >
                {/* Header line: Icon + Label + Count Badge */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2.5 font-medium text-foreground">
                    <IconComponent className="h-4 w-4 text-destructive shrink-0" />
                    <span>{labelText}</span>
                  </div>
                  <Badge variant="destructive" className="font-bold">
                    {dep.count}
                  </Badge>
                </div>

                {/* Example pills presentation (if available) */}
                {hasExamples && (
                  <div className="flex flex-wrap items-center gap-1.5 pt-1 border-t border-border/50 text-xs">
                    {dep.examples!.map((ex, idx) => (
                      <span
                        key={idx}
                        dir="auto"
                        className="px-2 py-0.5 rounded-md bg-secondary text-secondary-foreground font-mono text-[11px] max-w-[200px] truncate"
                      >
                        {ex}
                      </span>
                    ))}

                    {remainingCount > 0 && (
                      <span className="text-muted-foreground text-[11px] font-medium px-1">
                        {isAr ? `و +${remainingCount} غيرها` : `+${remainingCount} more`}
                      </span>
                    )}
                  </div>
                )}
              </Card>
            );
          })}
        </div>

        <AlertDialogFooter className="sm:justify-end">
          <AlertDialogAction
            onClick={() => onOpenChange(false)}
            className="w-full sm:w-auto px-5"
          >
            {isAr ? 'إغلاق' : 'Close'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
