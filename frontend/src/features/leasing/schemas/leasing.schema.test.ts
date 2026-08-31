import test from 'node:test';
import assert from 'node:assert/strict';
import { createLeaseContractSchema } from './leasing.schema';
import { LegalRegime, PaymentFrequency, TenantType } from '../types/leasing.types';

const validLease = {
  apartmentId: '11111111-1111-4111-8111-111111111111',
  tenantId: '22222222-2222-4222-8222-222222222222',
  contractNumber: 'LEASE-TEST-001',
  startDate: '2026-09-01',
  endDate: '2027-09-01',
  monthlyRentAmount: 350,
  securityDepositAmount: 0,
  paymentFrequency: PaymentFrequency.Monthly,
  paymentDueDay: 28,
  legalRegime: LegalRegime.Standard,
  tenantType: TenantType.Personal,
  notes: '',
};

test('lease payment due day matches the backend and database range of 1 through 28', () => {
  assert.equal(createLeaseContractSchema.safeParse(validLease).success, true);
  assert.equal(createLeaseContractSchema.safeParse({ ...validLease, paymentDueDay: 29 }).success, false);
});
