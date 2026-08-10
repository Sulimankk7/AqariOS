import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { Input } from '@/app/components/ui/input';
import { Card, CardHeader, CardTitle, CardContent } from '@/app/components/ui/card';
import { useApartments, useDeleteApartment } from '../hooks/useApartments';
import { useBuildings } from '@/features/buildings/hooks/useBuildings';
import { ApartmentDto } from '../types/apartments.types';
import { BuildingDto } from '@/features/buildings/types/buildings.types';
import { getApartmentTranslation } from '../constants/translations';
import { ownershipStatusToLabel, occupancyStatusToLabel, OccupancyStatus } from '../constants/apartmentEnums';
import { useTranslation } from '@/shared/i18n';
import {
  Eye,
  Edit,
  Trash,
  Plus,
  Building2,
  ChevronDown,
  ChevronUp,
  Search,
} from 'lucide-react';
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

interface BuildingGroup {
  buildingId: string;
  buildingName: string;
  internalCode?: string;
  apartments: ApartmentDto[];
  totalCount: number;
  occupiedCount: number;
  vacantCount: number;
  otherStatusCount: number;
}

export function ApartmentsList({ buildingId, floorId }: ApartmentsListProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getApartmentTranslation(key, language);

  const { data: apartments, isLoading: isLoadingApartments, error: errorApartments } = useApartments({ buildingId, floorId });
  const { data: buildings, isLoading: isLoadingBuildings } = useBuildings();
  const deleteMutation = useDeleteApartment();
  
  const [selectedApartmentForDelete, setSelectedApartmentForDelete] = useState<ApartmentDto | null>(null);
  const [archiveBlockedData, setArchiveBlockedData] = useState<ArchiveBlockedPayload | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');
  
  // Track collapsed state per building group (buildingId -> boolean)
  const [collapsedMap, setCollapsedMap] = useState<Record<string, boolean>>({});

  const isLoading = isLoadingApartments || isLoadingBuildings;

  // Map building lookup dictionary
  const buildingMap = useMemo(() => {
    const map = new Map<string, BuildingDto>();
    buildings?.forEach((b) => map.set(b.id, b));
    return map;
  }, [buildings]);

  // Filter apartments by global search term
  const filteredApartments = useMemo(() => {
    if (!apartments) return [];
    if (!searchQuery.trim()) return apartments;
    const q = searchQuery.toLowerCase().trim();
    return apartments.filter((apt) => {
      const bld = buildingMap.get(apt.buildingId);
      const bldName = bld?.name?.toLowerCase() || '';
      const unit = apt.unitNumber.toLowerCase();
      const status = occupancyStatusToLabel(apt.occupancyStatus, t).toLowerCase();
      const rent = apt.baseRentAmount ? apt.baseRentAmount.toString() : '';
      return unit.includes(q) || bldName.includes(q) || status.includes(q) || rent.includes(q);
    });
  }, [apartments, searchQuery, buildingMap, t]);

  // Group apartments by Building
  const buildingGroups = useMemo(() => {
    const groupsMap = new Map<string, ApartmentDto[]>();

    // If a specific buildingId prop is passed, seed that building first
    if (buildingId) {
      groupsMap.set(buildingId, []);
    }

    // Seed all known buildings from buildingMap so even buildings with 0 apartments appear if listing
    if (!floorId && !buildingId && buildings) {
      buildings.forEach((b) => {
        if (!groupsMap.has(b.id)) {
          groupsMap.set(b.id, []);
        }
      });
    }

    filteredApartments.forEach((apt) => {
      const id = apt.buildingId || 'unassigned';
      if (!groupsMap.has(id)) {
        groupsMap.set(id, []);
      }
      groupsMap.get(id)!.push(apt);
    });

    const result: BuildingGroup[] = [];

    groupsMap.forEach((apts, bldId) => {
      // Don't show empty building sections if user is actively searching and 0 units match in this building
      if (searchQuery.trim() && apts.length === 0) {
        return;
      }

      const bld = buildingMap.get(bldId);
      const buildingName = bld ? bld.name : (bldId === 'unassigned' ? t('unassignedBuilding') : `${t('buildingId')}: ${bldId}`);
      const internalCode = bld?.internalCode;

      const totalCount = apts.length;
      const occupiedCount = apts.filter((a) => a.occupancyStatus === OccupancyStatus.Occupied).length;
      const vacantCount = apts.filter((a) => a.occupancyStatus === OccupancyStatus.Vacant).length;
      const otherStatusCount = totalCount - occupiedCount - vacantCount;

      result.push({
        buildingId: bldId,
        buildingName,
        internalCode,
        apartments: apts,
        totalCount,
        occupiedCount,
        vacantCount,
        otherStatusCount,
      });
    });

    return result;
  }, [filteredApartments, buildingMap, buildings, buildingId, floorId, searchQuery, t]);

  const toggleGroupCollapse = (id: string) => {
    setCollapsedMap((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  const handleExpandAll = () => setCollapsedMap({});
  const handleCollapseAll = () => {
    const allCollapsed: Record<string, boolean> = {};
    buildingGroups.forEach((g) => {
      allCollapsed[g.buildingId] = true;
    });
    setCollapsedMap(allCollapsed);
  };

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id, {
      onSuccess: () => setSelectedApartmentForDelete(null),
      onError: (err: any) => {
        const payload = extractArchiveBlockedPayload(err);
        if (payload) {
          setSelectedApartmentForDelete(null);
          setArchiveBlockedData(payload);
        }
      },
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
          className="font-medium text-primary cursor-pointer hover:underline flex items-center gap-1.5" 
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

  if (errorApartments) {
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

      {/* Global Toolbar: Search & Group Toggle Controls */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-card p-4 rounded-lg border shadow-xs">
        <div className="relative w-full sm:w-80">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground rtl:left-auto rtl:right-3" />
          <Input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('searchPlaceholder')}
            className="pl-9 rtl:pl-3 rtl:pr-9 text-xs"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
          <Button variant="outline" size="sm" onClick={handleExpandAll} className="text-xs">
            {t('expandAll')}
          </Button>
          <Button variant="outline" size="sm" onClick={handleCollapseAll} className="text-xs">
            {t('collapseAll')}
          </Button>
        </div>
      </div>

      {/* Building Sections Container */}
      {buildingGroups.length === 0 ? (
        <Card className="p-8 text-center text-muted-foreground text-sm">
          {t('noApartments')}
        </Card>
      ) : (
        <div className="space-y-6">
          {buildingGroups.map((group) => {
            // Auto-expand if active search query matches units in this building
            const isSearching = searchQuery.trim().length > 0;
            const isCollapsed = isSearching ? false : !!collapsedMap[group.buildingId];

            return (
              <Card key={group.buildingId} className="overflow-hidden border shadow-xs">
                {/* Sticky Section Header */}
                <CardHeader 
                  className="sticky top-0 z-10 bg-card border-b py-3.5 px-4 sm:px-6 flex flex-row items-center justify-between cursor-pointer select-none hover:bg-muted/50 transition-colors"
                  onClick={() => toggleGroupCollapse(group.buildingId)}
                >
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-md bg-primary/10 text-primary">
                      <Building2 className="w-4 h-4" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <CardTitle className="text-base font-bold text-foreground">
                          {group.buildingName}
                        </CardTitle>
                        {group.internalCode && (
                          <Badge variant="outline" className="text-[10px] font-mono">
                            {group.internalCode}
                          </Badge>
                        )}
                      </div>

                      {/* Compact Summary Metrics Bar */}
                      <div className="flex items-center gap-3 text-xs text-muted-foreground mt-1">
                        <span>
                          <strong className="text-foreground">{group.totalCount}</strong> {t('apartments')}
                        </span>
                        <span>•</span>
                        <span className="text-emerald-600 dark:text-emerald-400 font-medium">
                          <strong>{group.occupiedCount}</strong> {t('occupiedApartments')}
                        </span>
                        <span>•</span>
                        <span className="text-amber-600 dark:text-amber-400 font-medium">
                          <strong>{group.vacantCount}</strong> {t('vacantApartments')}
                        </span>
                      </div>
                    </div>
                  </div>

                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground"
                    aria-label={isCollapsed ? 'Expand section' : 'Collapse section'}
                  >
                    {isCollapsed ? (
                      <ChevronDown className="w-4 h-4" />
                    ) : (
                      <ChevronUp className="w-4 h-4" />
                    )}
                  </Button>
                </CardHeader>

                {/* Collapsible Content: Apartments Table */}
                {!isCollapsed && (
                  <CardContent className="p-0">
                    <DataTable
                      data={group.apartments}
                      columns={columns}
                      isLoading={isLoading}
                      emptyMessage={t('noApartments')}
                    />
                  </CardContent>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {/* Delete Apartment Confirmation Dialog */}
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
