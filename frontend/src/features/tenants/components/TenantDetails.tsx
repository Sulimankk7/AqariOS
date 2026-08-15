import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Badge } from '@/app/components/ui/badge';
import { DataTable, Column } from '@/shared/components/ui/DataTable';
import {
  TenantDetailDto,
  TenantFamilyMemberDto,
  TenantEmergencyContactDto,
  TenantVehicleDto,
  LeaseContractDto,
} from '../types/tenants.types';
import { useTenantLeaseHistory } from '../hooks/useTenants';
import { getTenantTranslation } from '../constants/translations';
import { useTranslation } from '@/shared/i18n';
import {
  User,
  Phone,
  Mail,
  Briefcase,
  Building,
  Users,
  ShieldAlert,
  Car,
  FileText,
  Edit,
  Trash2,
  Eye,
  ArrowLeft,
  ArrowRight,
  Plus,
} from 'lucide-react';
import { contractStatusToLabel, contractStatusToBadgeVariant } from '@/features/leasing/constants/leasingEnums';
import { getLeasingTranslation } from '@/features/leasing/constants/translations';
import { DeleteTenantDialog } from './DeleteTenantDialog';
import { FamilyMemberFormDialog } from './FamilyMemberFormDialog';
import { DeleteFamilyMemberDialog } from './DeleteFamilyMemberDialog';
import { EmergencyContactFormDialog } from './EmergencyContactFormDialog';
import { DeleteEmergencyContactDialog } from './DeleteEmergencyContactDialog';
import { VehicleFormDialog } from './VehicleFormDialog';
import { DeleteVehicleDialog } from './DeleteVehicleDialog';
import { ProvisionTenantAccountModal } from './ProvisionTenantAccountModal';
import { useFamilyMembers } from '../hooks/useFamilyMembers';
import { useEmergencyContacts } from '../hooks/useEmergencyContacts';
import { useVehicles } from '../hooks/useVehicles';
import { extractUserFriendlyError } from '@/shared/utils';
import { UserPlus, CheckCircle2, ShieldCheck, KeyRound } from 'lucide-react';

interface TenantDetailsProps {
  tenant: TenantDetailDto;
}

export function TenantDetails({ tenant }: TenantDetailsProps) {
  const navigate = useNavigate();
  const { language } = useTranslation();
  const t = (key: string) => getTenantTranslation(key, language);
  const leasingT = (key: string) => getLeasingTranslation(key, language);

  const [deletingTenant, setDeletingTenant] = useState<boolean>(false);
  const [isAddingFamilyMember, setIsAddingFamilyMember] = useState<boolean>(false);
  const [editingFamilyMember, setEditingFamilyMember] = useState<TenantFamilyMemberDto | null>(null);
  const [deletingFamilyMember, setDeletingFamilyMember] = useState<TenantFamilyMemberDto | null>(null);

  const [isAddingEmergencyContact, setIsAddingEmergencyContact] = useState<boolean>(false);
  const [editingEmergencyContact, setEditingEmergencyContact] = useState<TenantEmergencyContactDto | null>(null);
  const [deletingEmergencyContact, setDeletingEmergencyContact] = useState<TenantEmergencyContactDto | null>(null);

  const [isAddingVehicle, setIsAddingVehicle] = useState<boolean>(false);
  const [editingVehicle, setEditingVehicle] = useState<TenantVehicleDto | null>(null);
  const [deletingVehicle, setDeletingVehicle] = useState<TenantVehicleDto | null>(null);
  const [isProvisioningAccount, setIsProvisioningAccount] = useState<boolean>(false);

  // Fetch dynamic family members list
  const { data: familyMembersList } = useFamilyMembers(tenant.id);
  const familyMembers = familyMembersList || tenant.familyMembers;

  // Fetch dynamic emergency contacts list
  const { data: emergencyContactsList } = useEmergencyContacts(tenant.id);
  const emergencyContacts = emergencyContactsList || tenant.emergencyContacts;

  // Fetch dynamic vehicles list
  const { data: vehiclesList } = useVehicles(tenant.id);
  const vehicles = vehiclesList || tenant.vehicles;

  // Fetch tenant lease history from endpoint GET /api/v1/leasing/tenants/{tenantId}/leases
  const {
    data: leases,
    isLoading: isLoadingLeases,
    error: errorLeases,
    refetch: refetchLeases,
  } = useTenantLeaseHistory(tenant.id, 50);

  // Columns for Lease History
  const leaseColumns: Column<LeaseContractDto>[] = [
    {
      key: 'contractNumber',
      header: leasingT('contractNumber'),
      accessor: (row) => row.contractNumber,
      cell: (row) => (
        <div
          className="font-medium text-primary cursor-pointer hover:underline"
          onClick={() => navigate(`/leases/${row.id}`)}
        >
          {row.contractNumber}
        </div>
      ),
    },
    {
      key: 'status',
      header: leasingT('status'),
      accessor: (row) => contractStatusToLabel(row.status, leasingT),
      cell: (row) => (
        <Badge variant={contractStatusToBadgeVariant(row.status)}>
          {contractStatusToLabel(row.status, leasingT)}
        </Badge>
      ),
    },
    {
      key: 'dates',
      header: `${leasingT('startDate')} — ${leasingT('endDate')}`,
      accessor: (row) => `${row.startDate} ~ ${row.endDate}`,
    },
    {
      key: 'monthlyRent',
      header: leasingT('monthlyRent'),
      accessor: (row) => `${row.monthlyRentAmount} ${row.currency}`,
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      cell: (row) => (
        <Button
          variant="ghost"
          size="icon"
          title={leasingT('viewDetails')}
          aria-label={leasingT('viewDetails')}
          onClick={() => navigate(`/leases/${row.id}`)}
        >
          <Eye className="w-4 h-4" />
        </Button>
      ),
    },
  ];

  const BackIcon = language === 'ar' ? ArrowRight : ArrowLeft;

  return (
    <div className="space-y-6">
      {/* Header Actions Card */}
      <div className="flex flex-wrap items-center justify-between gap-4 bg-card p-6 rounded-lg border shadow-xs">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" aria-label="Back to Tenants" onClick={() => navigate('/tenants')}>
            <BackIcon className="w-5 h-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-foreground">{tenant.name}</h1>
            <p className="text-sm text-muted-foreground mt-0.5">
              {t('nationalId')}: {tenant.nationalId}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => navigate(`/tenants/${tenant.id}/edit`)}>
            <Edit className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('edit')}
          </Button>

          <Button variant="destructive" onClick={() => setDeletingTenant(true)}>
            <Trash2 className="w-4 h-4 mr-2 rtl:ml-2 rtl:mr-0" />
            {t('delete')}
          </Button>
        </div>
      </div>

      {/* Primary Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Basic Personal Information */}
        <Card className="md:col-span-3">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <User className="w-5 h-5 text-primary" />
              {t('tenantDetails')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('name')}</span>
              <span className="text-sm font-semibold">{tenant.name}</span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('nationalId')}</span>
              <span className="text-sm font-semibold">{tenant.nationalId}</span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('phone')}</span>
              <span className="text-sm font-semibold flex items-center gap-1.5" dir="ltr">
                <Phone className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                {tenant.phone}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('email')}</span>
              <span className="text-sm font-semibold flex items-center gap-1.5" dir="ltr">
                <Mail className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                {tenant.email || '—'}
              </span>
            </div>

            <div className="border p-3 rounded-md">
              <span className="text-xs text-muted-foreground block">{t('occupation')}</span>
              <span className="text-sm font-medium flex items-center gap-1.5">
                <Briefcase className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                {tenant.occupation || '—'}
              </span>
            </div>

            <div className="border p-3 rounded-md sm:col-span-2">
              <span className="text-xs text-muted-foreground block">{t('employer')}</span>
              <span className="text-sm font-medium flex items-center gap-1.5">
                <Building className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                {tenant.employer || '—'}
              </span>
            </div>
          </CardContent>
        </Card>

        {/* Tenant Portal Account Status Card */}
        <Card className="md:col-span-3">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <div className="space-y-1">
              <CardTitle className="text-lg flex items-center gap-2">
                <KeyRound className="w-5 h-5 text-primary" />
                {language === 'ar' ? 'حساب بوابة المستأجر' : 'Tenant Portal Account'}
              </CardTitle>
              <CardDescription className="text-xs text-muted-foreground">
                {tenant.userId
                  ? language === 'ar'
                    ? 'حساب بوابة المستأجر مرتبط ومفعل بهذا المستأجر.'
                    : 'Tenant portal account is linked to this tenant record.'
                  : language === 'ar'
                  ? 'حساب بوابة المستأجر غير مفعل. يمكنك إنشاء حساب للمستأجر وإرسال رابط التفعيل إليه.'
                  : 'Tenant portal account is not activated. You can create an account and send an activation link to the tenant.'}
              </CardDescription>
            </div>
            <div>
              {tenant.userId ? (
                <Badge variant="outline" className="bg-emerald-500/10 text-emerald-600 border-emerald-500/30 gap-1.5 text-xs py-1 px-3">
                  <ShieldCheck className="w-4 h-4" />
                  {language === 'ar' ? 'حساب مرتبط / مفعل' : 'Account Linked'}
                </Badge>
              ) : (
                <Button
                  variant="default"
                  size="sm"
                  onClick={() => setIsProvisioningAccount(true)}
                  className="gap-2 font-semibold"
                >
                  <UserPlus className="w-4 h-4" />
                  {language === 'ar' ? 'إنشاء حساب المستأجر' : 'Create Tenant Account'}
                </Button>
              )}
            </div>
          </CardHeader>
        </Card>
      </div>

      {/* Sub-Entities Section: Family Members, Emergency Contacts, Vehicles */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Family Members */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <Users className="w-4 h-4 text-primary" />
              {t('familyMembers')}
            </CardTitle>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsAddingFamilyMember(true)}
              className="h-7 text-xs gap-1"
            >
              <Plus className="w-3.5 h-3.5" />
              {t('addFamilyMember')}
            </Button>
          </CardHeader>
          <CardContent>
            {familyMembers.length === 0 ? (
              <p className="text-xs text-muted-foreground text-center py-4">{t('noFamilyMembers')}</p>
            ) : (
              <div className="space-y-2">
                {familyMembers.map((member) => (
                  <div key={member.id} className="p-2.5 border rounded-md text-xs space-y-1.5 group">
                    <div className="flex items-center justify-between">
                      <div className="font-semibold text-foreground">{member.name}</div>
                      <div className="flex items-center gap-1 opacity-90 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('editFamilyMember')}
                          aria-label={t('editFamilyMember')}
                          onClick={() => setEditingFamilyMember(member)}
                        >
                          <Edit className="w-3.5 h-3.5 text-muted-foreground hover:text-foreground" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('deleteFamilyMember')}
                          aria-label={t('deleteFamilyMember')}
                          onClick={() => setDeletingFamilyMember(member)}
                        >
                          <Trash2 className="w-3.5 h-3.5 text-destructive hover:text-destructive/80" />
                        </Button>
                      </div>
                    </div>
                    <div className="text-muted-foreground flex justify-between">
                      <span>{t('relationship')}: {member.relationshipType}</span>
                      {member.ageBracket && <span>{member.ageBracket}</span>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Emergency Contacts */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <ShieldAlert className="w-4 h-4 text-primary" />
              {t('emergencyContacts')}
            </CardTitle>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsAddingEmergencyContact(true)}
              className="h-7 text-xs gap-1"
            >
              <Plus className="w-3.5 h-3.5" />
              {t('addEmergencyContact')}
            </Button>
          </CardHeader>
          <CardContent>
            {emergencyContacts.length === 0 ? (
              <p className="text-xs text-muted-foreground text-center py-4">{t('noEmergencyContacts')}</p>
            ) : (
              <div className="space-y-2">
                {emergencyContacts.map((contact) => (
                  <div key={contact.id} className="p-2.5 border rounded-md text-xs space-y-1.5 group">
                    <div className="flex items-center justify-between">
                      <div className="font-semibold text-foreground">{contact.name}</div>
                      <div className="flex items-center gap-1 opacity-90 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('editEmergencyContact')}
                          aria-label={t('editEmergencyContact')}
                          onClick={() => setEditingEmergencyContact(contact)}
                        >
                          <Edit className="w-3.5 h-3.5 text-muted-foreground hover:text-foreground" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('deleteEmergencyContact')}
                          aria-label={t('deleteEmergencyContact')}
                          onClick={() => setDeletingEmergencyContact(contact)}
                        >
                          <Trash2 className="w-3.5 h-3.5 text-destructive hover:text-destructive/80" />
                        </Button>
                      </div>
                    </div>
                    <div className="text-muted-foreground flex justify-between">
                      <span>{contact.relationshipType}</span>
                      <span className="font-mono">{contact.phone}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Vehicles */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
            <CardTitle className="text-base flex items-center gap-2">
              <Car className="w-4 h-4 text-primary" />
              {t('vehicles')}
            </CardTitle>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsAddingVehicle(true)}
              className="h-7 text-xs gap-1"
            >
              <Plus className="w-3.5 h-3.5" />
              {t('addVehicle')}
            </Button>
          </CardHeader>
          <CardContent>
            {vehicles.length === 0 ? (
              <p className="text-xs text-muted-foreground text-center py-4">{t('noVehicles')}</p>
            ) : (
              <div className="space-y-2">
                {vehicles.map((v) => (
                  <div key={v.id} className="p-2.5 border rounded-md text-xs space-y-1.5 group">
                    <div className="flex items-center justify-between">
                      <div className="font-semibold text-foreground flex items-center gap-2">
                        <span>{v.makeModel}</span>
                        <Badge variant="outline" className="text-[10px] font-mono">{v.plateNumber}</Badge>
                      </div>
                      <div className="flex items-center gap-1 opacity-90 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('editVehicle')}
                          aria-label={t('editVehicle')}
                          onClick={() => setEditingVehicle(v)}
                        >
                          <Edit className="w-3.5 h-3.5 text-muted-foreground hover:text-foreground" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          title={t('deleteVehicle')}
                          aria-label={t('deleteVehicle')}
                          onClick={() => setDeletingVehicle(v)}
                        >
                          <Trash2 className="w-3.5 h-3.5 text-destructive hover:text-destructive/80" />
                        </Button>
                      </div>
                    </div>
                    <div className="text-muted-foreground">{t('color')}: {v.color}</div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Lease History Card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <FileText className="w-5 h-5 text-primary" />
            {t('leaseHistory')}
          </CardTitle>
          <CardDescription>All lease contracts associated with this tenant.</CardDescription>
        </CardHeader>
        <CardContent>
          {errorLeases ? (
            <div className="p-4 text-center space-y-2">
              <p className="text-destructive font-medium">
                {extractUserFriendlyError(errorLeases, t('loadError'))}
              </p>
              <Button variant="outline" size="sm" onClick={() => refetchLeases()}>
                {t('retry')}
              </Button>
            </div>
          ) : (
            <DataTable
              data={leases || []}
              columns={leaseColumns}
              isLoading={isLoadingLeases}
              emptyMessage={t('noLeases')}
            />
          )}
        </CardContent>
      </Card>

      {/* Delete Tenant Confirmation Modal */}
      <DeleteTenantDialog
        tenantId={deletingTenant ? tenant.id : null}
        tenantName={tenant.name}
        open={deletingTenant}
        onOpenChange={(open) => {
          setDeletingTenant(open);
        }}
      />

      {/* Family Member Add / Edit Form Modal */}
      <FamilyMemberFormDialog
        tenantId={tenant.id}
        open={isAddingFamilyMember || !!editingFamilyMember}
        onOpenChange={(open) => {
          if (!open) {
            setIsAddingFamilyMember(false);
            setEditingFamilyMember(null);
          }
        }}
        initialData={editingFamilyMember}
      />

      {/* Delete Family Member Confirmation Modal */}
      <DeleteFamilyMemberDialog
        tenantId={tenant.id}
        member={deletingFamilyMember}
        open={!!deletingFamilyMember}
        onOpenChange={(open) => {
          if (!open) {
            setDeletingFamilyMember(null);
          }
        }}
      />

      {/* Emergency Contact Add / Edit Form Modal */}
      <EmergencyContactFormDialog
        tenantId={tenant.id}
        open={isAddingEmergencyContact || !!editingEmergencyContact}
        onOpenChange={(open) => {
          if (!open) {
            setIsAddingEmergencyContact(false);
            setEditingEmergencyContact(null);
          }
        }}
        initialData={editingEmergencyContact}
      />

      {/* Delete Emergency Contact Confirmation Modal */}
      <DeleteEmergencyContactDialog
        tenantId={tenant.id}
        contact={deletingEmergencyContact}
        open={!!deletingEmergencyContact}
        onOpenChange={(open) => {
          if (!open) {
            setDeletingEmergencyContact(null);
          }
        }}
      />

      {/* Vehicle Add / Edit Form Modal */}
      <VehicleFormDialog
        tenantId={tenant.id}
        open={isAddingVehicle || !!editingVehicle}
        onOpenChange={(open) => {
          if (!open) {
            setIsAddingVehicle(false);
            setEditingVehicle(null);
          }
        }}
        initialData={editingVehicle}
      />

      {/* Delete Vehicle Confirmation Modal */}
      <DeleteVehicleDialog
        tenantId={tenant.id}
        vehicle={deletingVehicle}
        open={!!deletingVehicle}
        onOpenChange={(open) => {
          if (!open) {
            setDeletingVehicle(null);
          }
        }}
      />

      {/* Provision Tenant Account Modal */}
      <ProvisionTenantAccountModal
        tenantId={tenant.id}
        tenantName={tenant.name}
        tenantPhone={tenant.phone}
        tenantEmail={tenant.email}
        open={isProvisioningAccount}
        onOpenChange={setIsProvisioningAccount}
      />
    </div>
  );
}
