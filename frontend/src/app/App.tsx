/**
 * AqariOS Root Component.
 * Wraps the application in global providers (AppProvider) and the client-side router (AppRouter).
 */

import { AppProvider } from "@/app/providers/AppProvider";
import { AppRouter } from "@/app/router/AppRouter";

export default function App() {
  return (
    <AppProvider>
      <AppRouter />
    </AppProvider>
  );
}
