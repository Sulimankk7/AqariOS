import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/app/components/ui/dialog';
import { Button } from '@/app/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { useCreateMaintenanceRequest } from '../hooks/useMaintenance';
import { MaintenanceCategory, MaintenancePriority } from '../types/maintenance.types';

// Hooks from existing domains
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { useApartments } from '@/features/apartments/hooks/useApartments';
import { useSearchTenants } from '@/features/tenants/hooks/useTenants';

interface CreateMaintenanceRequestDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

const CATEGORY_TRANSLATIONS: Record<MaintenanceCategory, string> = {
  [MaintenanceCategory.Electrical]: 'كهرباء',
  [MaintenanceCategory.Plumbing]: 'سباكة',
  [MaintenanceCategory.AirConditioning]: 'تكييف',
  [MaintenanceCategory.Elevator]: 'مصاعد',
  [MaintenanceCategory.Cleaning]: 'نظافة',
  [MaintenanceCategory.Water]: 'مياه',
  [MaintenanceCategory.Structural]: 'إنشائي',
  [MaintenanceCategory.DoorsWindows]: 'أبواب ونوافذ',
  [MaintenanceCategory.Internet]: 'إنترنت',
  [MaintenanceCategory.Other]: 'أخرى',
};

const PRIORITY_TRANSLATIONS: Record<MaintenancePriority, string> = {
  [MaintenancePriority.Low]: 'منخفض',
  [MaintenancePriority.Medium]: 'متوسط',
  [MaintenancePriority.High]: 'مرتفع',
  [MaintenancePriority.Emergency]: 'عاجل',
};

export const CreateMaintenanceRequestDialog = ({ open, onOpenChange }: CreateMaintenanceRequestDialogProps) => {
  const { mutate: createRequest, isPending: isCreating } = useCreateMaintenanceRequest();

  const [formData, setFormData] = useState({
    title: '',
    description: '',
    buildingId: '',
    apartmentId: '',
    tenantId: '',
    category: MaintenanceCategory.Other,
    priority: MaintenancePriority.Low,
  });

  // Data fetching
  const { data: buildings, isLoading: isLoadingBuildings } = useBuildings();
  
  const { data: apartments, isLoading: isLoadingApartments } = useApartments(
    formData.buildingId ? { buildingId: formData.buildingId } : undefined
  );

  const { data: tenants, isLoading: isLoadingTenants } = useSearchTenants();

  // Reset dependents when building changes
  const handleBuildingChange = (val: string) => {
    setFormData(prev => ({
      ...prev,
      buildingId: val,
      apartmentId: '',
      tenantId: ''
    }));
  };

  // Reset tenant when apartment changes
  const handleApartmentChange = (val: string) => {
    setFormData(prev => ({
      ...prev,
      apartmentId: val,
      tenantId: ''
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const today = new Date().toISOString().split('T')[0];

    createRequest({
      title: formData.title,
      description: formData.description,
      buildingId: formData.buildingId,
      apartmentId: formData.apartmentId || undefined,
      tenantId: formData.tenantId || undefined,
      category: formData.category,
      priority: formData.priority,
      requestDate: today
    }, {
      onSuccess: () => {
        onOpenChange(false);
        setFormData({
          title: '',
          description: '',
          buildingId: '',
          apartmentId: '',
          tenantId: '',
          category: MaintenanceCategory.Other,
          priority: MaintenancePriority.Low,
        });
      }
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>إنشاء طلب صيانة جديد</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">عنوان طلب الصيانة *</label>
              <input
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                required
                value={formData.title}
                onChange={(e) => setFormData(p => ({ ...p, title: e.target.value }))}
              />
            </div>
            
            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">العقار / العمارة *</label>
              <Select
                value={formData.buildingId}
                onValueChange={handleBuildingChange}
                disabled={isLoadingBuildings}
                required
              >
                <SelectTrigger>
                  <SelectValue placeholder="اختر العقار" />
                </SelectTrigger>
                <SelectContent>
                  {buildings?.map(b => (
                    <SelectItem key={b.id} value={b.id}>{b.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">الشقة / الوحدة (اختياري)</label>
              <Select
                value={formData.apartmentId}
                onValueChange={handleApartmentChange}
                disabled={!formData.buildingId || isLoadingApartments}
              >
                <SelectTrigger>
                  <SelectValue placeholder="اختر الشقة" />
                </SelectTrigger>
                <SelectContent>
                  {apartments?.map(a => (
                    <SelectItem key={a.id} value={a.id}>
                      {a.unitNumber}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">المستأجر (اختياري)</label>
              <Select
                value={formData.tenantId}
                onValueChange={(val) => setFormData(p => ({ ...p, tenantId: val }))}
                disabled={!formData.buildingId || isLoadingTenants}
              >
                <SelectTrigger>
                  <SelectValue placeholder="اختر المستأجر" />
                </SelectTrigger>
                <SelectContent>
                  {tenants?.map(t => (
                    <SelectItem key={t.id} value={t.id}>{t.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">نوع المشكلة *</label>
              <Select
                value={formData.category.toString()}
                onValueChange={(val: string) => setFormData(p => ({ ...p, category: parseInt(val, 10) as MaintenanceCategory }))}
                required
              >
                <SelectTrigger>
                  <SelectValue placeholder="اختر الفئة" />
                </SelectTrigger>
                <SelectContent>
                  {[
                    MaintenanceCategory.Electrical,
                    MaintenanceCategory.Plumbing,
                    MaintenanceCategory.AirConditioning,
                    MaintenanceCategory.Elevator,
                    MaintenanceCategory.Cleaning,
                    MaintenanceCategory.Water,
                    MaintenanceCategory.Structural,
                    MaintenanceCategory.DoorsWindows,
                    MaintenanceCategory.Internet,
                    MaintenanceCategory.Other,
                  ].map(cat => (
                    <SelectItem key={cat} value={cat.toString()}>
                      {CATEGORY_TRANSLATIONS[cat]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">الأولوية *</label>
              <Select
                value={formData.priority.toString()}
                onValueChange={(val: string) => setFormData(p => ({ ...p, priority: parseInt(val, 10) as MaintenancePriority }))}
                required
              >
                <SelectTrigger>
                  <SelectValue placeholder="اختر الأولوية" />
                </SelectTrigger>
                <SelectContent>
                  {[
                    MaintenancePriority.Low,
                    MaintenancePriority.Medium,
                    MaintenancePriority.High,
                    MaintenancePriority.Emergency,
                  ].map(priority => (
                    <SelectItem key={priority} value={priority.toString()}>
                      {PRIORITY_TRANSLATIONS[priority]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2 col-span-2">
              <label className="text-sm font-medium">وصف المشكلة *</label>
              <textarea
                className="w-full min-h-[80px] rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                required
                value={formData.description}
                onChange={(e) => setFormData(p => ({ ...p, description: e.target.value }))}
              />
            </div>
          </div>

          <DialogFooter className="mt-6">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isCreating}>
              إلغاء
            </Button>
            <Button type="submit" disabled={isCreating || !formData.buildingId || !formData.title || !formData.description}>
              {isCreating ? 'جاري الإنشاء...' : 'إنشاء الطلب'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};
