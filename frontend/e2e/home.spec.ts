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

  await page.setViewportSize({ width: 390, height: 844 });
  await runs.screenshot({ path: 'e2e/screenshots/runs-phone.png', animations: 'disabled' });
});

test('BEH-HIGH-1: each song shows the minute of the run it started at', async ({ page }) => {
  await page.route('**/api/runs', (route) =>
    route.fulfill({
      json: [
        {
          activityId: 3, name: 'Long Run', sportType: 'Run', startedAt: '2026-10-16T18:00:00Z',
          elapsedSeconds: 4000,
          songs: [
            // Already playing when the run started, so its offset is negative.
            { name: 'Warm Up', artists: ['Artist A'], albumArtUrl: null, playedAt: '2026-10-16T18:02:00Z', offsetSeconds: -60 },
            { name: 'Middle', artists: ['Artist B'], albumArtUrl: null, playedAt: '2026-10-16T18:16:00Z', offsetSeconds: 760 },
            { name: 'Late', artists: ['Artist C'], albumArtUrl: null, playedAt: '2026-10-16T19:05:00Z', offsetSeconds: 3725 },
          ],
        },
      ],
    }),
  );
  await page.goto('./');

  const songs = page.getByRole('region', { name: 'Runs' }).locator('ol > li');
  await expect(songs).toHaveCount(3);
  await expect(songs.nth(0)).toContainText('0:00 Warm Up');
  await expect(songs.nth(1)).toContainText('12:40 Middle');
  await expect(songs.nth(2)).toContainText('1:02:05 Late');

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('region', { name: 'Runs' }).screenshot({ path: 'e2e/screenshots/runs-song-minutes-phone.png', animations: 'disabled' });
});

test('BEH-HIGH-3 / BEH-HIGH-4: the top-speed and fastest-pace songs are labelled, others are not', async ({ page }) => {
  await page.route('**/api/runs', (route) =>
    route.fulfill({
      json: [
        {
          activityId: 4, name: 'Hill Run', sportType: 'Run', startedAt: '2026-10-16T18:00:00Z',
          elapsedSeconds: 1800,
          songs: [
            { name: 'Plain', artists: ['Artist A'], albumArtUrl: null, playedAt: '2026-10-16T18:04:00Z', offsetSeconds: 0, isTopSpeed: false, isFastestPace: false },
            { name: 'Sprinter', artists: ['Artist B'], albumArtUrl: null, playedAt: '2026-10-16T18:08:00Z', offsetSeconds: 240, isTopSpeed: true, isFastestPace: false },
            { name: 'Steady', artists: ['Artist C'], albumArtUrl: null, playedAt: '2026-10-16T18:12:00Z', offsetSeconds: 480, isTopSpeed: false, isFastestPace: true },
            { name: 'Both', artists: ['Artist D'], albumArtUrl: null, playedAt: '2026-10-16T18:16:00Z', offsetSeconds: 720, isTopSpeed: true, isFastestPace: true },
          ],
        },
      ],
    }),
  );
  await page.goto('./');

  const songs = page.getByRole('region', { name: 'Runs' }).locator('ol > li');
  await expect(songs).toHaveCount(4);
  await expect(songs.nth(0)).not.toContainText('top speed');
  await expect(songs.nth(0)).not.toContainText('fastest pace');
  await expect(songs.nth(1)).toContainText('⚡ top speed');
  await expect(songs.nth(1)).not.toContainText('fastest pace');
  await expect(songs.nth(2)).toContainText('fastest pace');
  await expect(songs.nth(2)).not.toContainText('top speed');
  await expect(songs.nth(3)).toContainText('⚡ top speed');
  await expect(songs.nth(3)).toContainText('fastest pace');

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('region', { name: 'Runs' }).screenshot({ path: 'e2e/screenshots/runs-highlights-phone.png', animations: 'disabled' });
});

// A 5k-ish loop in a park, as [lat, lng]: the shape the API sends after thinning (BEH-MAP-2).
const loop = Array.from({ length: 120 }, (_, i) => {
  const t = (i / 119) * 2 * Math.PI;
  return [51.5045 + 0.006 * Math.sin(t) + 0.0015 * Math.sin(3 * t), -0.1605 + 0.011 * Math.cos(t)];
});
const cover = (colour: string) =>
  `data:image/svg+xml,${encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="64" height="64" fill="${colour}"/></svg>`)}`;

test('BEH-MAP-1 / BEH-MAP-2 / BEH-MAP-3: a run draws its route with each song pinned where it started; a run without GPS draws none', async ({ page }) => {
  await page.route('**/api/runs', (route) =>
    route.fulfill({
      json: [
        {
          activityId: 5, name: 'Park Loop', sportType: 'Run', startedAt: '2026-10-27T07:00:00Z',
          elapsedSeconds: 1680, contextType: 'playlist', contextName: 'Morning Miles', route: loop,
          songs: [
            { name: 'Opener', artists: ['Artist A'], albumArtUrl: cover('#e11d48'), playedAt: '2026-10-27T07:04:00Z', offsetSeconds: 0, isTopSpeed: false, isFastestPace: false, routeIndex: 0 },
            { name: 'Sprinter', artists: ['Artist B'], albumArtUrl: cover('#f59e0b'), playedAt: '2026-10-27T07:08:00Z', offsetSeconds: 240, isTopSpeed: true, isFastestPace: false, routeIndex: 17 },
            { name: 'Steady', artists: ['Artist C'], albumArtUrl: null, playedAt: '2026-10-27T07:12:00Z', offsetSeconds: 480, isTopSpeed: false, isFastestPace: true, routeIndex: 34 },
            { name: 'Closer', artists: ['Artist D'], albumArtUrl: cover('#10b981'), playedAt: '2026-10-27T07:16:00Z', offsetSeconds: 720, isTopSpeed: false, isFastestPace: false, routeIndex: 51 },
          ],
        },
        {
          activityId: 6, name: 'Treadmill', sportType: 'Run', startedAt: '2026-10-26T07:00:00Z',
          elapsedSeconds: 1200, contextType: null, contextName: null, route: [],
          songs: [
            { name: 'Indoor', artists: ['Artist E'], albumArtUrl: null, playedAt: '2026-10-26T07:04:00Z', offsetSeconds: 0, isTopSpeed: false, isFastestPace: false, routeIndex: null },
          ],
        },
      ],
    }),
  );
  await page.goto('./');

  const runs = page.getByRole('region', { name: 'Runs' }).locator('ul > li');
  await expect(runs).toHaveCount(2);
  await expect(runs.nth(0).getByTestId('route-map')).toBeVisible();
  await expect(runs.nth(0).getByTestId('song-pin')).toHaveCount(4);
  await expect(runs.nth(0).getByRole('img', { name: 'Route with 4 songs pinned' })).toBeVisible();
  await expect(runs.nth(1).getByTestId('route-map')).toHaveCount(0);
  await expect(runs.nth(1)).toContainText('Indoor');

  await page.setViewportSize({ width: 390, height: 844 });
  await runs.nth(0).screenshot({ path: 'e2e/screenshots/runs-map-phone.png', animations: 'disabled' });
});

test('BEH-HIGH-2: a run names the album or playlist it ran to, or says a type, mixed, or nothing', async ({ page }) => {
  const song = { name: 'Song', artists: ['Artist A'], albumArtUrl: null, playedAt: '2026-10-16T18:04:00Z', offsetSeconds: 0, isTopSpeed: false, isFastestPace: false };
  const run = (activityId: number, name: string, songs: object[], contextType: string | null, contextName: string | null) => ({
    activityId, name, sportType: 'Run', startedAt: '2026-10-16T18:00:00Z', elapsedSeconds: 1800, songs, contextType, contextName,
  });
  await page.route('**/api/runs', (route) =>
    route.fulfill({
      json: [
        run(1, 'Named', [song], 'album', 'Abbey Road'),
        run(2, 'Unnamed', [song], 'playlist', null),
        run(3, 'Mixed', [song], 'mixed', null),
        run(4, 'Before', [song], null, null),
        run(5, 'Silent', [], 'album', 'Ignored'),
      ],
    }),
  );
  await page.goto('./');

  const runs = page.getByRole('region', { name: 'Runs' }).locator('ul > li').filter({ has: page.locator('h3') });
  await expect(runs.nth(0)).toContainText('Run to: Abbey Road');
  await expect(runs.nth(1)).toContainText('Run to: a playlist');
  await expect(runs.nth(2)).toContainText('Run to: a mix of songs');
  await expect(runs.nth(3)).not.toContainText('Run to');
  await expect(runs.nth(4)).not.toContainText('Run to');

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('region', { name: 'Runs' }).screenshot({ path: 'e2e/screenshots/runs-context-phone.png', animations: 'disabled' });
});

test('BEH-SYNC-4: home page explains an empty run list', async ({ page }) => {
  await page.goto('./');

  await expect(
    page.getByRole('region', { name: 'Runs' }).getByText('No runs yet', { exact: false }),
  ).toBeVisible();
  await expect(page.getByText('after the next one finishes on Strava', { exact: false })).toBeVisible();
});
