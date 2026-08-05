import React from 'react';
import { LeaseContractsList } from '../components/LeaseContractsList';

export function LeasesPage() {
  return (
    <div className="container mx-auto py-8 px-4">
      <LeaseContractsList />
    </div>
  );
}

export default LeasesPage;
