import { defineConfig } from "vitest/config";
import path from "node:path";
export default defineConfig({
  esbuild: { jsx: "automatic" },
  resolve: { alias: { "@": path.resolve(__dirname, "src") } },
  test: {
    environment: "jsdom",
    include: ["src/features/mvp/*.mvp.test.tsx"],
    setupFiles: ["src/features/mvp/test-setup.ts"],
    restoreMocks: true,
  },
});
