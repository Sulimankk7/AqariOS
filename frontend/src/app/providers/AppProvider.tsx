/**
 * AppProvider — Global provider wrapper component for AqariOS foundation.
 * Combines ThemeProvider, I18nProvider, QueryClientProvider, and AuthProvider.
 */

import React, { useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/shared/theme";
import { I18nProvider } from "@/shared/i18n";
import { AuthProvider } from "@/features/auth/providers/AuthProvider";
import { Toaster } from "@/shared/ui/sonner";

interface AppProviderProps {
  children: React.ReactNode;
}

export function AppProvider({ children }: AppProviderProps) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 300000, // 5 minutes cache
            refetchOnWindowFocus: false,
            retry: 2,
          },
        },
      })
  );

  return (
    <ThemeProvider defaultTheme="system">
      <I18nProvider defaultLanguage="ar">
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            {children}
            <Toaster />
          </AuthProvider>
        </QueryClientProvider>
      </I18nProvider>
    </ThemeProvider>
  );
}
