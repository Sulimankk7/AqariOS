import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { useBuildings, useDeleteBuilding } from '../hooks/useBuildings';
import { BuildingDto } from '../types/buildings.types';
import { getBuildingTranslation } from '../constants/translations';
import { buildingTypeToLabel, governorateToLabel } from '../constants/buildingEnums';
import { useTranslation } from '@/shared/i18n';
import { Eye, Edit, Trash } from 'lucide-react';
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

export function BuildingsList() {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getBuildingTranslation(key, language);

  const { data: buildings, isLoading, error } = useBuildings();
  const deleteMutation = useDeleteBuilding();
  
  const [selectedBuildingForDelete, setSelectedBuildingForDelete] = useState<BuildingDto | null>(null);
  const [archiveBlockedData, setArchiveBlockedData] = useState<ArchiveBlockedPayload | null>(null);

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: () => setSelectedBuildingForDelete(null),
      onError: (err: any) => {
        const payload = extractArchiveBlockedPayload(err);
        if (payload) {
          setSelectedBuildingForDelete(null);
          setArchiveBlockedData(payload);
        }
      }
    });
  };

  const columns: Column<BuildingDto>[] = [
    {
      key: 'name',
      header: t('name'),
      accessor: (row) => row.name,
      sortable: true,
      cell: (row) => (
        <div className="font-medium text-primary cursor-pointer hover:underline" onClick={() => navigate(`/buildings/${row.id}`)}>
          {row.name}
        </div>
      )
    },
    {
      key: 'internalCode',
      header: t('tableCode'),
      accessor: (row) => row.internalCode || '-',
      sortable: true
    },
    {
      key: 'buildingType',
      header: t('buildingType'),
      accessor: (row) => buildingTypeToLabel(row.buildingType, t),
      sortable: true,
      cell: (row) => <Badge variant="secondary">{buildingTypeToLabel(row.buildingType, t)}</Badge>
    },
    {
      key: 'totalFloors',
      header: t('tableFloors'),
      accessor: (row) => row.totalFloors,
      sortable: true
    },
    {
      key: 'apartments',
      header: t('tableApartments'),
      accessor: (row) => row.totalApartmentsCount,
      sortable: true
    },
    {
      key: 'location',
      header: t('tableLocation'),
      accessor: (row) => row.address ? `${row.address.district ? row.address.district + ', ' : ''}${governorateToLabel(row.address.governorate, t)}` : '-',
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
            title={t('viewBuilding')}
            aria-label={t('viewBuilding')}
            onClick={() => navigate(`/buildings/${row.id}`)}
          >
            <Eye className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={t('editBuildingTitle')}
            aria-label={t('editBuildingTitle')}
            onClick={() => navigate(`/buildings/${row.id}/edit`)}
          >
            <Edit className="w-4 h-4" />
          </Button>
          <Button 
            variant="ghost" 
            size="icon" 
            title={t('deleteBuildingTitle')}
            aria-label={t('deleteBuildingTitle')}
            className="text-destructive hover:text-destructive hover:bg-destructive/10" 
            onClick={() => setSelectedBuildingForDelete(row)}
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
        title={t('buildings')} 
        description={t('pageDescription')}
        actions={
          <Button onClick={() => navigate('/buildings/new')}>
            {t('addBuilding')}
          </Button>
        }
      />
      
      <DataTable
        data={buildings || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder={t('searchPlaceholder')}
      />

      <AlertDialog open={!!selectedBuildingForDelete} onOpenChange={(open) => !open && setSelectedBuildingForDelete(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('deleteConfirmTitle')}: {selectedBuildingForDelete?.name}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('deleteConfirmMessage')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('cancel')}</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => selectedBuildingForDelete && handleDelete(selectedBuildingForDelete.id)}
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
