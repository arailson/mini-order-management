import { defineConfig, devices } from '@playwright/test';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const rootDir = resolve(import.meta.dirname, '..');
const apiUrl = process.env.VITE_API_URL ?? 'http://localhost:5000';
const frontUrl = 'http://localhost:5173';

function loadRootEnv(): Record<string, string> {
  const envPath = resolve(rootDir, '.env');
  if (!existsSync(envPath)) return {};
  const result: Record<string, string> = {};
  for (const line of readFileSync(envPath, 'utf8').split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const idx = trimmed.indexOf('=');
    if (idx <= 0) continue;
    result[trimmed.slice(0, idx)] = trimmed.slice(idx + 1);
  }
  return result;
}

const rootEnv = loadRootEnv();
const connectionString =
  process.env.ConnectionStrings__DefaultConnection ??
  rootEnv.ConnectionStrings__DefaultConnection?.replace('Server=sqlserver', 'Server=localhost') ??
  '';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: frontUrl,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project backend/src/API --launch-profile http',
      cwd: rootDir,
      url: `${apiUrl}/clientes`,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: {
        ...process.env,
        DOTNET_ROLL_FORWARD: 'Major',
        ASPNETCORE_ENVIRONMENT: 'Development',
        ...(connectionString
          ? { ConnectionStrings__DefaultConnection: connectionString }
          : {}),
      },
    },
    {
      command: 'npm run dev --prefix frontend',
      cwd: rootDir,
      url: frontUrl,
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
});
