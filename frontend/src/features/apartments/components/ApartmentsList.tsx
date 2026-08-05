import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { useApartments, useDeleteApartment } from '../hooks/useApartments';
import { ApartmentDto } from '../types/apartments.types';
import { getApartmentTranslation } from '../constants/translations';
import { ownershipStatusToLabel, occupancyStatusToLabel, OccupancyStatus } from '../constants/apartmentEnums';
import { useTranslation } from '@/shared/i18n';
import { Eye, Edit, Trash, Plus } from 'lucide-react';
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
import { PageHeader } from '@/shared/components/ui/Headers';
import { ArchiveBlockedDialog } from '@/shared/components/ui/ArchiveBlockedDialog';
import { extractArchiveBlockedPayload, ArchiveBlockedPayload } from '@/shared/lib/archiveBlocked';

interface ApartmentsListProps {
  buildingId?: string;
  floorId?: string;
}

export function ApartmentsList({ buildingId, floorId }: ApartmentsListProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);

  const { data: apartments, isLoading, error } = useApartments({ buildingId, floorId });
  const deleteMutation = useDeleteApartment();
  
  const [selectedApartmentForDelete, setSelectedApartmentForDelete] = useState<ApartmentDto | null>(null);
  const [archiveBlockedData, setArchiveBlockedData] = useState<ArchiveBlockedPayload | null>(null);

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: () => setSelectedApartmentForDelete(null),
      onError: (err: any) => {
        const payload = extractArchiveBlockedPayload(err);
        if (payload) {
          setSelectedApartmentForDelete(null);
          setArchiveBlockedData(payload);
        }
      }
    });
  };

  const getOccupancyBadgeVariant = (status: OccupancyStatus) => {
    switch (status) {
      case OccupancyStatus.Occupied: return 'default';
      case OccupancyStatus.Vacant: return 'secondary';
      case OccupancyStatus.UnderMaintenance: return 'destructive';
      case OccupancyStatus.Listed: return 'outline';
      default: return 'secondary';
    }
  };

  const columns: Column<ApartmentDto>[] = [
    {
      key: 'unitNumber',
      header: t('tableUnit'),
      accessor: (row) => row.unitNumber,
      sortable: true,
      cell: (row) => (
        <div 
          className="font-medium text-primary cursor-pointer hover:underline" 
          onClick={() => navigate(`/apartments/${row.id}`)}
        >
          {row.unitNumber}
        </div>
      )
    },
    {
      key: 'areaSqm',
      header: t('tableArea'),
      accessor: (row) => `${row.areaSqm} m²`,
      sortable: true
    },
    {
      key: 'rooms',
      header: `${t('bedrooms')} / ${t('bathrooms')}`,
      accessor: (row) => `${row.bedrooms} BDR | ${row.bathrooms} BTH`,
    },
    {
      key: 'occupancyStatus',
      header: t('tableOccupancy'),
      accessor: (row) => occupancyStatusToLabel(row.occupancyStatus, t),
      sortable: true,
      cell: (row) => (
        <Badge variant={getOccupancyBadgeVariant(row.occupancyStatus)}>
          {occupancyStatusToLabel(row.occupancyStatus, t)}
        </Badge>
      )
    },
    {
      key: 'ownershipStatus',
      header: t('tableOwnership'),
      accessor: (row) => ownershipStatusToLabel(row.ownershipStatus, t),
      sortable: true,
      cell: (row) => (
        <Badge variant="outline">
          {ownershipStatusToLabel(row.ownershipStatus, t)}
        </Badge>
      )
    },
    {
      key: 'baseRent',
      header: t('tableBaseRent'),
      accessor: (row) => row.baseRentAmount ? `${row.baseRentAmount} ${row.baseRentCurrency}` : '-',
      sortable: true
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
            title={`${t('view')} Unit ${row.unitNumber}`}
            aria-label={`${t('view')} Unit ${row.unitNumber}`}
            onClick={() => navigate(`/apartments/${row.id}`)}
          >
            <Eye className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={`${t('edit')} Unit ${row.unitNumber}`}
            aria-label={`${t('edit')} Unit ${row.unitNumber}`}
            onClick={() => navigate(`/apartments/${row.id}/edit`)}
          >
            <Edit className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={`${t('delete')} Unit ${row.unitNumber}`}
            aria-label={`${t('delete')} Unit ${row.unitNumber}`}
            className="text-destructive hover:text-destructive hover:bg-destructive/10" 
            onClick={() => setSelectedApartmentForDelete(row)}
          >
            <Trash className="w-4 h-4" />
          </Button>
        </div>
      )
    }
  ];

  if (error) {
    return <div className="p-8 text-center text-destructive">{t('loadError')}</div>;
  }

  return (
    <div className="space-y-6">
      <PageHeader 
        title={t('apartments')} 
        description={t('pageDescription')}
        actions={
          floorId ? (
            <Button onClick={() => navigate(`/floors/${floorId}/apartments/new`)}>
              <Plus className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
              {t('addApartment')}
            </Button>
          ) : undefined
        }
      />
      
      <DataTable
        data={apartments || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder={t('searchPlaceholder')}
      />

      <AlertDialog open={!!selectedApartmentForDelete} onOpenChange={(open) => {
        if (!open) {
          setSelectedApartmentForDelete(null);
        }
      }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {t('deleteConfirmTitle')} — Unit {selectedApartmentForDelete?.unitNumber}?
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t('deleteConfirmMessage')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('cancel')}</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                if (selectedApartmentForDelete) {
                  handleDelete(selectedApartmentForDelete.id);
                }
              }}
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
