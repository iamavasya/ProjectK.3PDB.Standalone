import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5220';

export default defineConfig({
  testDir: './tests',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    video: 'retain-on-failure',
  },

  // Bring up the mock-update container automatically (unless the app is already
  // running or PLAYWRIGHT_NO_WEBSERVER is set, in which case the existing one is reused).
  webServer: process.env.PLAYWRIGHT_NO_WEBSERVER
    ? undefined
    : {
        command: 'docker compose --profile e2e up --build -d',
        url: `${baseURL}/api/update/readiness`,
        reuseExistingServer: true,
        timeout: 180_000,
        cwd: '..',
      },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
