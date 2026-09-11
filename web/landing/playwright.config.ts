import { defineConfig } from "@playwright/test";

// Port 3000 is occupied by an unrelated app on this dev machine, so this
// project's dev server always runs on 3100 instead (see README).
const PORT = 3100;

export default defineConfig({
  testDir: "./tests",
  webServer: {
    command: `npm run dev -- --port ${PORT}`,
    url: `http://localhost:${PORT}`,
    reuseExistingServer: true,
    timeout: 30_000,
  },
  use: {
    baseURL: `http://localhost:${PORT}`,
  },
});
