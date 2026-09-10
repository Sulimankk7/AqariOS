import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  esbuild: { jsx: "automatic" },
  resolve: { alias: { "@": path.resolve(__dirname, "src") } },
  test: {
    environment: "jsdom",
    include: ["src/features/platformAdmin/**/*.test.tsx"],
    setupFiles: ["src/features/platformAdmin/test-setup.ts"],
    restoreMocks: true,
  },
});
