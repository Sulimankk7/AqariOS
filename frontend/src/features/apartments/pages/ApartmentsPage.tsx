import React from 'react';
import { useSearchParams } from 'react-router';
import { ApartmentsList } from '../components/ApartmentsList';

export default function ApartmentsPage() {
  const [searchParams] = useSearchParams();
  const buildingId = searchParams.get('buildingId') || undefined;
  const floorId = searchParams.get('floorId') || undefined;

  return (
    <div className="container mx-auto py-8 px-4">
      <ApartmentsList buildingId={buildingId} floorId={floorId} />
    </div>
  );
}
