import { test, expect } from '@playwright/test';
import { mockApi } from './mocks';

/**
 * Smoke + screenshot coverage for the scaffold's home page, driven off mocked
 * API responses. Each test asserts the key content is present, then captures a
 * full-page screenshot into e2e/screenshots/ for visual review.
 *
 * This is the reusable e2e/screenshot pattern for Northstar frontends: mock
 * every API call in one place (mocks.ts), assert, then screenshot.
 */

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test('BEH-BRAND-1: home page renders the Tracks hero', async ({ page }) => {
  await page.goto('./');

  await expect(page).toHaveTitle('Tracks');
  await expect(
    page.getByRole('heading', { name: 'Tracks', level: 1 }),
  ).toBeVisible();
  await expect(page.getByText("Track your tracks' tracks.", { exact: false })).toBeVisible();

  await page.screenshot({
    path: 'e2e/screenshots/home-light.png',
    fullPage: true,
    // Fast-forward the theme transition so renders are deterministic instead
    // of catching a half-faded frame right after the toggle click.
    animations: 'disabled',
  });
});

test('home page renders in dark mode', async ({ page }) => {
  await page.goto('./');
  await expect(
    page.getByRole('heading', { name: 'Tracks', level: 1 }),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Toggle Theme' }).click();

  await page.screenshot({
    path: 'e2e/screenshots/home-dark.png',
    fullPage: true,
    // Fast-forward the theme transition so renders are deterministic instead
    // of catching a half-faded frame right after the toggle click.
    animations: 'disabled',
  });
});

test('BEH-SYNC-4: home page lists each run with its songs, and says when a run has none', async ({ page }) => {
  // Registered after mockApi's default, so this one wins.
  await page.route('**/api/runs', (route) =>
    route.fulfill({
      json: [
        {
          activityId: 2, name: 'Evening Run', sportType: 'Run', startedAt: '2026-10-16T18:00:00Z',
          elapsedSeconds: 1925,
          songs: [
            { name: 'Song One', artists: ['Artist A'], albumArtUrl: null, playedAt: '2026-10-16T18:01:00Z', offsetSeconds: 60 },
            { name: 'Song Two', artists: ['Artist B', 'Artist C'], albumArtUrl: null, playedAt: '2026-10-16T18:05:00Z', offsetSeconds: 300 },
          ],
        },
        {
          activityId: 1, name: 'Quiet Run', sportType: 'Run', startedAt: '2026-10-15T07:00:00Z',
          elapsedSeconds: 3725, songs: [],
        },
      ],
    }),
  );
  await page.goto('./');

  const runs = page.getByRole('region', { name: 'Runs' });
  await expect(runs.getByRole('heading', { name: 'Evening Run' })).toBeVisible();
  await expect(runs.getByText('32:05')).toBeVisible();
  await expect(runs.getByText('Song One')).toBeVisible();
  await expect(runs.getByText('Artist B, Artist C')).toBeVisible();
  await expect(runs.getByRole('heading', { name: 'Quiet Run' })).toBeVisible();
  await expect(runs.getByText('1:02:05')).toBeVisible();
  await expect(runs.getByText('No songs found for this run.')).toBeVisible();
});

test('BEH-SYNC-4: home page explains an empty run list', async ({ page }) => {
  await page.goto('./');

  await expect(
    page.getByRole('region', { name: 'Runs' }).getByText('No runs yet', { exact: false }),
  ).toBeVisible();
  await expect(page.getByText('after the next one finishes on Strava', { exact: false })).toBeVisible();
});
