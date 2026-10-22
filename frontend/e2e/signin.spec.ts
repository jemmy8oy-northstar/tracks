import { test, expect } from '@playwright/test';
import { mockApi, mockMe } from './mocks';

/** Multi-user sign-in: the Home page for signed-out, signed-in-without-Spotify and fully connected runners. */

const runs = [
  {
    activityId: 2, name: 'Evening Run', sportType: 'Run', startedAt: '2026-10-16T18:00:00Z',
    elapsedSeconds: 1925,
    songs: [{ name: 'Song One', artists: ['Artist A'], albumArtUrl: null, playedAt: '2026-10-16T18:01:00Z', offsetSeconds: 60 }],
  },
];

test('BEH-AUTH-1: signed out shows a Strava sign-in link and never asks for runs', async ({ page }) => {
  await mockApi(page, null);
  let runsRequested = false;
  await page.route('**/api/runs', (route) => {
    runsRequested = true;
    return route.fulfill({ status: 401, json: { title: 'Unauthorized' } });
  });
  await page.goto('./');

  const signIn = page.getByRole('link', { name: 'Sign in with Strava' });
  await expect(signIn).toBeVisible();
  await expect(signIn).toHaveAttribute('href', '/tracks/api/auth/strava');
  await expect(page.getByText('Tracks reads your runs from Strava', { exact: false })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Tracks', level: 1 })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Runs' })).toHaveCount(0);
  await expect(page.getByText('Could not reach the API')).toHaveCount(0);
  expect(runsRequested).toBe(false);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'e2e/screenshots/signed-out-phone.png', fullPage: true, animations: 'disabled' });
});

test('BEH-CONNECT-4: arriving with ?error=full tells a signed-out runner Tracks is full, not an error', async ({ page }) => {
  await mockApi(page, null);
  await page.goto('./?error=full');

  await expect(page.getByRole('alert')).toContainText('Tracks is full for now');
  await expect(page.getByText('Could not reach the API')).toHaveCount(0);

  // Control: the normal signed-out page carries no such message.
  await page.goto('./');
  await expect(page.getByRole('link', { name: 'Sign in with Strava' })).toBeVisible();
  await expect(page.getByText('Tracks is full for now')).toHaveCount(0);
});

test('BEH-AUTH-2: signed in without Spotify offers Connect Spotify and still lists runs', async ({ page }) => {
  await mockApi(page, { athleteId: 1, spotifyConnected: false });
  await page.route('**/api/runs', (route) => route.fulfill({ json: runs }));
  await page.goto('./');

  const connect = page.getByRole('link', { name: 'Connect Spotify' });
  await expect(connect).toBeVisible();
  await expect(connect).toHaveAttribute('href', '/tracks/api/auth/spotify');
  await expect(page.getByText("Songs can't be matched", { exact: false })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Runs' }).getByRole('heading', { name: 'Evening Run' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Disconnect Spotify' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Sign in with Strava' })).toHaveCount(0);
});

test('BEH-AUTH-3: signed in with Spotify shows runs, and Sign out returns to the signed-out page', async ({ page }) => {
  await mockApi(page);
  await page.route('**/api/runs', (route) => route.fulfill({ json: runs }));
  let signOutPosted = false;
  await page.route('**/api/auth/signout', async (route) => {
    signOutPosted = route.request().method() === 'POST';
    await mockMe(page, null);
    await route.fulfill({ status: 200, json: {} });
  });
  await page.goto('./');

  await expect(page.getByRole('region', { name: 'Runs' }).getByRole('heading', { name: 'Evening Run' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Connect Spotify' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Disconnect Spotify' })).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page.getByRole('link', { name: 'Sign in with Strava' })).toBeVisible();
  expect(signOutPosted).toBe(true);
});
