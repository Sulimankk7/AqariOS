import React from 'react';
import { TenantsList } from '../components/TenantsList';

export function TenantsPage() {
  return (
    <div className="container mx-auto py-8 px-4">
      <TenantsList />
    </div>
  );
}

export default TenantsPage;
