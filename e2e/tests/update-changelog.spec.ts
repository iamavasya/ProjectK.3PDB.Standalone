import { test, expect } from '@playwright/test';

/**
 * Verifies the full "previous version -> update -> restart -> changelog" flow using the
 * mocked update service. The changelog must open after the (simulated) restart and show
 * the exact test value we configured for the new version.
 */

const OLD_VERSION = '1.0.0';
const NEW_VERSION = '2.0.0';
const CHANGELOG_MARKER = 'E2E_CHANGELOG_MARKER';
const NEW_NOTES = `## Що нового\n\n${CHANGELOG_MARKER} — версія ${NEW_VERSION}`;

test.beforeEach(async ({ request }) => {
  // Put the mock into a deterministic "running old version, new version available" state,
  // so the test is fully re-runnable regardless of prior runs.
  await request.post('/api/testctl/version', { data: { version: OLD_VERSION } });
  await request.post('/api/testctl/available', { data: { version: NEW_VERSION } });
  await request.post('/api/testctl/release-notes', {
    data: { version: NEW_VERSION, notes: NEW_NOTES },
  });
});

test('update -> restart -> changelog shows the configured test value', async ({ page }) => {
  await page.goto('/');

  // The app reports the previous version.
  await expect(page.getByText(`v${OLD_VERSION}`)).toBeVisible();

  // The update banner advertises the new version.
  await expect(page.getByText('Доступна нова версія')).toBeVisible();
  await expect(page.getByText(NEW_VERSION, { exact: false })).toBeVisible();

  // No changelog is shown on the first run (no prior version recorded yet).
  await expect(page.getByRole('dialog').filter({ hasText: 'Що нового' })).toBeHidden();

  // Drive the real update UI: download, then restart (apply).
  await page.getByRole('button', { name: 'Завантажити' }).click();
  await page.getByRole('button', { name: 'Перезапустити' }).click();

  // The frontend applies the update, waits for the backend to report the new version,
  // then reloads itself. After that reload, the changelog opens with our test value.
  const changelog = page.getByRole('dialog').filter({ hasText: 'Що нового' });
  await expect(changelog).toBeVisible({ timeout: 30_000 });
  await expect(changelog).toContainText(CHANGELOG_MARKER);
  await expect(changelog).toContainText(NEW_VERSION);

  // The app now runs the new version.
  await expect(page.getByText(`v${NEW_VERSION}`)).toBeVisible();

  // Closing the changelog records the new version as "seen".
  await changelog.getByRole('button', { name: 'Зрозуміло' }).click();
  await expect(changelog).toBeHidden();
});

test('changelog can be driven directly via the test-control endpoint', async ({ page, request }) => {
  // Alternative, UI-independent path: seed old version, load (records it), then flip the
  // running version via simulate-update and reload to trigger the changelog.
  await request.post('/api/testctl/version', { data: { version: OLD_VERSION } });
  await page.goto('/');
  await expect(page.getByText(`v${OLD_VERSION}`)).toBeVisible();

  const marker = 'DIRECT_TRIGGER_MARKER';
  await request.post('/api/testctl/simulate-update', {
    data: { toVersion: NEW_VERSION, notes: `## Реліз\n\n${marker}` },
  });

  await page.reload();

  const changelog = page.getByRole('dialog').filter({ hasText: 'Що нового' });
  await expect(changelog).toBeVisible({ timeout: 30_000 });
  await expect(changelog).toContainText(marker);
});
