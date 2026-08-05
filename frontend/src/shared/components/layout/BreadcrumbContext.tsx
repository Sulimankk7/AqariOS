import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';

interface BreadcrumbContextType {
  breadcrumbTitles: Record<string, string>;
  setBreadcrumbTitle: (id: string, title: string) => void;
}

const BreadcrumbContext = createContext<BreadcrumbContextType>({
  breadcrumbTitles: {},
  setBreadcrumbTitle: () => {},
});

export function BreadcrumbProvider({ children }: { children: React.ReactNode }) {
  const [breadcrumbTitles, setBreadcrumbTitles] = useState<Record<string, string>>({});

  const setBreadcrumbTitle = useCallback((id: string, title: string) => {
    setBreadcrumbTitles((prev) => {
      if (prev[id] === title) return prev;
      return { ...prev, [id]: title };
    });
  }, []);

  return (
    <BreadcrumbContext.Provider value={{ breadcrumbTitles, setBreadcrumbTitle }}>
      {children}
    </BreadcrumbContext.Provider>
  );
}

export function useBreadcrumbTitles() {
  return useContext(BreadcrumbContext);
}

export function useSetBreadcrumbTitle(id?: string, title?: string) {
  const { setBreadcrumbTitle } = useContext(BreadcrumbContext);
  useEffect(() => {
    if (id && title) {
      setBreadcrumbTitle(id, title);
    }
  }, [id, title, setBreadcrumbTitle]);
}
