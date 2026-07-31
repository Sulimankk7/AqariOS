import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { useBuildings, useDeleteBuilding } from '../hooks/useBuildings';
import { BuildingDto } from '../types/buildings.types';
import { buildingTranslations as t } from '../constants/translations';
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

export function BuildingsList() {
  const navigate = useNavigate();
  const { data: buildings, isLoading, error } = useBuildings();
  const deleteMutation = useDeleteBuilding();
  
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: () => setDeleteId(null)
    });
  };

  const columns: Column<BuildingDto>[] = [
    {
      key: 'name',
      header: 'Name',
      accessor: (row) => row.name,
      sortable: true,
      cell: (row) => (
        <div className="font-medium text-primary cursor-pointer hover:underline" onClick={() => navigate(`/properties/buildings/${row.id}`)}>
          {row.name}
        </div>
      )
    },
    {
      key: 'internalCode',
      header: 'Code',
      accessor: (row) => row.internalCode || '-',
      sortable: true
    },
    {
      key: 'buildingType',
      header: 'Type',
      accessor: (row) => row.buildingType,
      sortable: true,
      cell: (row) => <Badge variant="secondary">{row.buildingType}</Badge>
    },
    {
      key: 'totalFloors',
      header: 'Floors',
      accessor: (row) => row.totalFloors,
      sortable: true
    },
    {
      key: 'apartments',
      header: 'Apartments',
      accessor: (row) => row.totalApartmentsCount,
      sortable: true
    },
    {
      key: 'location',
      header: 'Location',
      accessor: (row) => row.address ? `${row.address.district}, ${row.address.governorate}` : '-',
      sortable: true
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      cell: (row) => (
        <div className="flex justify-end gap-2">
          <Button variant="ghost" size="icon" onClick={() => navigate(`/properties/buildings/${row.id}`)}>
            <Eye className="w-4 h-4" />
          </Button>
          <Button variant="ghost" size="icon" onClick={() => navigate(`/properties/buildings/${row.id}/edit`)}>
            <Edit className="w-4 h-4" />
          </Button>
          <Button variant="ghost" size="icon" className="text-destructive hover:text-destructive hover:bg-destructive/10" onClick={() => setDeleteId(row.id)}>
            <Trash className="w-4 h-4" />
          </Button>
        </div>
      )
    }
  ];

  if (error) {
    return <div className="p-8 text-center text-destructive">{t.loadError}</div>;
  }

  return (
    <div className="space-y-6">
      <PageHeader 
        title={t.buildings} 
        description="Manage your property buildings and locations."
        actions={
          <Button onClick={() => navigate('/properties/buildings/create')}>
            {t.addBuilding}
          </Button>
        }
      />
      
      <DataTable
        data={buildings || []}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder="Search buildings by name or code..."
      />

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t.deleteConfirmTitle}</AlertDialogTitle>
            <AlertDialogDescription>
              {t.deleteConfirmMessage}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction 
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => deleteId && handleDelete(deleteId)}
            >
              {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
