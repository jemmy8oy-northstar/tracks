import { test, expect } from '@playwright/test';
import { mockApi, mockMe } from './mocks';

/** BEH-SHARE-2: the opt-in for writing a run's songs into its Strava activity description. */

test('BEH-SHARE-2: the setting is off by default and turning it on tells the API', async ({ page }) => {
  await mockApi(page, { athleteId: 1, spotifyConnected: true, stravaWriteGranted: true, writeSongsToStravaDescription: false });
  let body: unknown;
  await page.route('**/api/me/strava-description', async (route) => {
    expect(route.request().method()).toBe('PUT');
    body = route.request().postDataJSON();
    await mockMe(page, { athleteId: 1, spotifyConnected: true, stravaWriteGranted: true, writeSongsToStravaDescription: true });
    await route.fulfill({ status: 204 });
  });
  await page.goto('./');

  const toggle = page.getByRole('checkbox', { name: 'Add songs to my Strava activity description' });
  await expect(toggle).toBeEnabled();
  await expect(toggle).not.toBeChecked();
  await expect(page.getByRole('link', { name: 'Reconnect Strava' })).toHaveCount(0);

  // The box follows the server (checked comes from /api/me), so it flips once the PUT and the refetch land.
  await toggle.click();
  await expect(toggle).toBeChecked();
  expect(body).toEqual({ enabled: true });
});

test('BEH-SHARE-2: without the Strava write grant the setting is disabled and offers Reconnect Strava', async ({ page }) => {
  await mockApi(page, { athleteId: 1, spotifyConnected: true, stravaWriteGranted: false, writeSongsToStravaDescription: false });
  let putSent = false;
  await page.route('**/api/me/strava-description', (route) => {
    putSent = true;
    return route.fulfill({ status: 204 });
  });
  await page.goto('./');

  await expect(page.getByRole('checkbox', { name: 'Add songs to my Strava activity description' })).toBeDisabled();
  const reconnect = page.getByRole('link', { name: 'Reconnect Strava' });
  await expect(reconnect).toBeVisible();
  await expect(reconnect).toHaveAttribute('href', '/tracks/api/auth/strava');
  expect(putSent).toBe(false);
});
