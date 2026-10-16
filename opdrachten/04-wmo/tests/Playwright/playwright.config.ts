import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: '.', testMatch: '*.spec.ts', workers: 1, retries: 0, timeout: 30000,
  reporter: [['list'], ['html', { outputFolder: 'artifacts/html', open: 'never' }], ['junit', { outputFile: 'artifacts/results.xml' }]],
  use: { baseURL: process.env.BASE_URL, ignoreHTTPSErrors: false, trace: 'off' }
});
