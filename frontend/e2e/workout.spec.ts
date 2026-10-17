import { test, expect } from '@playwright/test';
import { mockApi } from './mocks';

/**
 * Phone-first coverage of the Workout page, off mocked API responses.
 * BEH-MAP-1: songs pinned along the route, and listed with the distance each covered.
 * BEH-MAP-3: a workout recorded without GPS lists its songs but draws no map.
 */

test.use({ viewport: { width: 390, height: 844 } });

test.beforeEach(async ({ page }) => {
  await mockApi(page);
});

test('BEH-MAP-1: a workout shows its songs pinned along the route and listed with distances', async ({ page }) => {
  await page.goto('./workouts/park-run');

  await expect(page.getByRole('heading', { name: 'Morning loop of Elm Park', level: 1 })).toBeVisible();
  await expect(page.getByTestId('route-map')).toBeVisible();

  // Every song is on the map and in the list, in the same order.
  await expect(page.getByTestId('song-pin')).toHaveCount(5);
  const rows = page.getByTestId('song-row');
  await expect(rows).toHaveCount(5);
  await expect(rows.nth(0)).toContainText('Tempo Lines');
  await expect(rows.nth(0)).toContainText('428 m');
  await expect(rows.nth(4)).toContainText('Mile Marker');
  await expect(rows.nth(4)).toContainText('1.21 km');
  await expect(rows.nth(2)).toContainText('at 8:00');

  // The pins are cover squares in the song's own colour, and the page fits a phone.
  await expect(page.getByTestId('song-pin').first().locator('rect')).toHaveAttribute('fill', '#e11d48');
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(0);

  await page.screenshot({
    path: '../docs/screenshots/workout-phone.png',
    fullPage: true,
    animations: 'disabled',
  });
});

test('BEH-MAP-3: a workout recorded without GPS lists its songs but draws no map', async ({ page }) => {
  await page.goto('./workouts/indoor-ride');

  await expect(page.getByRole('heading', { name: 'Indoor ride', level: 1 })).toBeVisible();
  await expect(page.getByTestId('song-row')).toHaveCount(3);
  await expect(page.getByTestId('song-row').nth(1)).toContainText('Cadence');
  await expect(page.getByTestId('song-row').nth(1)).toContainText('at 4:00');

  await expect(page.getByTestId('route-map')).toHaveCount(0);
  await expect(page.getByTestId('song-pin')).toHaveCount(0);

  await page.screenshot({
    path: 'e2e/screenshots/workout-indoor-phone.png',
    fullPage: true,
    animations: 'disabled',
  });
});

test('the workouts list links to each workout', async ({ page }) => {
  await page.goto('./workouts');

  await page.getByRole('link', { name: /Morning loop of Elm Park/ }).click();
  await expect(page.getByTestId('route-map')).toBeVisible();
});
