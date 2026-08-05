import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { useFloors, useDeleteFloor } from '../hooks/useFloors';
import { FloorDto } from '../types/floors.types';
import { getFloorTranslation } from '../constants/translations';
import { floorTypeToLabel } from '../constants/floorEnums';
import { useTranslation } from '@/shared/i18n';
import { Eye, Edit, Trash, Plus, Layers } from 'lucide-react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/app/components/ui/alert-dialog";
import { ArchiveBlockedDialog } from '@/shared/components/ui/ArchiveBlockedDialog';
import { extractArchiveBlockedPayload, ArchiveBlockedPayload } from '@/shared/lib/archiveBlocked';

interface FloorsListProps {
  buildingId: string;
}

export function FloorsList({ buildingId }: FloorsListProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getFloorTranslation(key, language);

  const { data: floors, isLoading, error } = useFloors(buildingId);
  const deleteMutation = useDeleteFloor();

  const [selectedFloorForDelete, setSelectedFloorForDelete] = useState<FloorDto | null>(null);
  const [archiveBlockedData, setArchiveBlockedData] = useState<ArchiveBlockedPayload | null>(null);

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: () => setSelectedFloorForDelete(null),
      onError: (err: any) => {
        const payload = extractArchiveBlockedPayload(err);
        if (payload) {
          setSelectedFloorForDelete(null);
          setArchiveBlockedData(payload);
        }
      },
    });
  };

  const columns: Column<FloorDto>[] = [
    {
      key: 'floorNumber',
      header: t('tableNumber'),
      accessor: (row) => `#${row.floorNumber}`,
      sortable: true,
      cell: (row) => (
        <div className="font-mono font-medium">#{row.floorNumber}</div>
      ),
    },
    {
      key: 'floorLabel',
      header: t('tableLabel'),
      accessor: (row) => row.floorLabel || `Floor ${row.floorNumber}`,
      sortable: true,
      cell: (row) => (
        <div 
          className="font-medium text-primary cursor-pointer hover:underline"
          onClick={() => navigate(`/buildings/${buildingId}/floors/${row.id}`)}
        >
          {row.floorLabel || `Floor ${row.floorNumber}`}
        </div>
      ),
    },
    {
      key: 'floorType',
      header: t('tableType'),
      accessor: (row) => floorTypeToLabel(row.floorType, t),
      cell: (row) => (
        <Badge variant="outline">
          {floorTypeToLabel(row.floorType, t)}
        </Badge>
      ),
    },
    {
      key: 'apartmentsCount',
      header: t('tableApartments'),
      accessor: (row) => row.apartmentsCount,
      sortable: true,
      cell: (row) => (
        <Badge variant="secondary">
          {row.apartmentsCount} units
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      cell: (row) => (
        <div className="flex justify-end gap-2">
          <Button 
            variant="ghost" 
            size="icon" 
            title={`${t('view')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            aria-label={`${t('view')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            onClick={() => navigate(`/buildings/${buildingId}/floors/${row.id}`)}
          >
            <Eye className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={`${t('edit')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            aria-label={`${t('edit')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            onClick={() => navigate(`/buildings/${buildingId}/floors/${row.id}/edit`)}
          >
            <Edit className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={`${t('delete')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            aria-label={`${t('delete')} ${row.floorLabel || 'Floor ' + row.floorNumber}`}
            className="text-destructive hover:text-destructive hover:bg-destructive/10" 
            onClick={() => setSelectedFloorForDelete(row)}
          >
            <Trash className="w-4 h-4" />
          </Button>
        </div>
      ),
    },
  ];

  if (error) {
    return <div className="p-8 text-center text-destructive">{t('loadError')}</div>;
  }

  if (!isLoading && (!floors || floors.length === 0)) {
    return (
      <div className="py-8 text-center border rounded-lg bg-card/40">
        <Layers className="h-8 w-8 text-muted-foreground/40 mx-auto mb-2" />
        <p className="text-sm text-muted-foreground font-medium">{t('noFloors')}</p>
        <Button 
          size="sm" 
          variant="outline" 
          className="mt-4 gap-1.5"
          onClick={() => navigate(`/buildings/${buildingId}/floors/new`)}
        >
          <Plus className="w-3.5 h-3.5" />
          {t('addFirstFloor')}
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <DataTable
        data={floors || []}
        columns={columns}
        isLoading={isLoading}
        searchable={false}
      />

      <AlertDialog open={!!selectedFloorForDelete} onOpenChange={(open) => !open && setSelectedFloorForDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {t('deleteConfirmTitle')} — {selectedFloorForDelete?.floorLabel || `Floor ${selectedFloorForDelete?.floorNumber}`}?
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t('deleteConfirmMessage')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('cancel')}</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => selectedFloorForDelete && handleDelete(selectedFloorForDelete.id)}
            >
              {deleteMutation.isPending ? t('deleting') : t('delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <ArchiveBlockedDialog
        open={!!archiveBlockedData}
        onOpenChange={(open) => !open && setArchiveBlockedData(null)}
        data={archiveBlockedData}
      />
    </div>
  );
}
