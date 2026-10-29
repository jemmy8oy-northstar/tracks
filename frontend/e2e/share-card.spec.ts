import { test, expect } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { mockApi } from './mocks';
import { CARD_HEIGHT, CARD_WIDTH, MAX_COVERS, ROUTE_BOX, layoutCovers, projectRoute } from '../src/lib/shareCard';

/**
 * BEH-SHARE-1. The repo has no unit-test runner (and adding one for two pure functions is not worth a
 * dependency), so the layout maths is tested here, in Playwright's own node runner, beside the browser test.
 */

const loop = Array.from({ length: 120 }, (_, i) => {
  const t = (i / 119) * 2 * Math.PI;
  return [51.5045 + 0.006 * Math.sin(t) + 0.0015 * Math.sin(3 * t), -0.1605 + 0.011 * Math.cos(t)];
});
const cover = (colour: string, label: string) =>
  `data:image/svg+xml,${encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="300" height="300"><rect width="300" height="300" fill="${colour}"/><circle cx="150" cy="150" r="90" fill="#00000040"/><text x="150" y="175" font-size="80" text-anchor="middle" fill="#fff" font-family="sans-serif">${label}</text></svg>`)}`;

test('route projection keeps the whole route inside the card box and centred (BEH-SHARE-1)', () => {
  const points = projectRoute(loop);
  expect(points).toHaveLength(loop.length);
  for (const p of points) {
    expect(p.x).toBeGreaterThanOrEqual(ROUTE_BOX.x - 1e-6);
    expect(p.x).toBeLessThanOrEqual(ROUTE_BOX.x + ROUTE_BOX.width + 1e-6);
    expect(p.y).toBeGreaterThanOrEqual(ROUTE_BOX.y - 1e-6);
    expect(p.y).toBeLessThanOrEqual(ROUTE_BOX.y + ROUTE_BOX.height + 1e-6);
  }
  // It touches the box on the limiting axis and is centred on the other.
  const xs = points.map((p) => p.x);
  const ys = points.map((p) => p.y);
  expect(Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys))).toBeCloseTo(ROUTE_BOX.width, 3);
  expect((Math.min(...xs) + Math.max(...xs)) / 2).toBeCloseTo(ROUTE_BOX.x + ROUTE_BOX.width / 2, 3);
  expect((Math.min(...ys) + Math.max(...ys)) / 2).toBeCloseTo(ROUTE_BOX.y + ROUTE_BOX.height / 2, 3);
});

test('route projection keeps north up and survives a single point or no route (BEH-SHARE-1)', () => {
  const [south, north] = projectRoute([[51.0, 0], [51.1, 0]]);
  expect(north.y).toBeLessThan(south.y);
  expect(projectRoute([[51, 0]])).toHaveLength(1);
  expect(projectRoute([])).toEqual([]);
});

test('cover layout puts every cover on the card without overlap, capped at the most a card holds (BEH-SHARE-1)', () => {
  for (const count of [0, 1, 3, 4, 5, 9, 12, 30]) {
    const slots = layoutCovers(count);
    expect(slots).toHaveLength(Math.min(count, MAX_COVERS));
    for (const [i, a] of slots.entries()) {
      expect(a.x).toBeGreaterThanOrEqual(0);
      expect(a.x + a.width).toBeLessThanOrEqual(CARD_WIDTH);
      expect(a.y).toBeGreaterThanOrEqual(ROUTE_BOX.y + ROUTE_BOX.height);
      expect(a.y + a.height).toBeLessThanOrEqual(CARD_HEIGHT);
      for (const b of slots.slice(i + 1)) {
        const apart = a.x + a.width <= b.x || b.x + b.width <= a.x || a.y + a.height <= b.y || b.y + b.height <= a.y;
        expect(apart).toBe(true);
      }
    }
  }
});

test.describe('share button', () => {
  test.beforeEach(async ({ page }) => {
    await mockApi(page);
  });

  test('a run with songs can be shared as a 1080x1920 card; a run without songs has no Share button (BEH-SHARE-1)', async ({ page }) => {
    const colours = ['#e11d48', '#f59e0b', '#10b981', '#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6'];
    await page.route('**/api/runs', (route) =>
      route.fulfill({
        json: [
          {
            activityId: 5, name: 'Park Loop', sportType: 'Run', startedAt: '2026-10-27T07:00:00Z',
            elapsedSeconds: 1680, contextType: null, contextName: null, route: loop,
            songs: colours.map((c, i) => ({
              name: `Song ${i + 1}`, artists: ['Artist'], albumArtUrl: i === 4 ? null : cover(c, String(i + 1)),
              playedAt: '2026-10-27T07:04:00Z', offsetSeconds: i * 240, isTopSpeed: false, isFastestPace: false,
              routeIndex: i * 16, distanceMetres: 900,
            })),
          },
          {
            activityId: 6, name: 'Quiet Run', sportType: 'Run', startedAt: '2026-10-26T07:00:00Z',
            elapsedSeconds: 1200, contextType: null, contextName: null, route: [], songs: [],
          },
        ],
      }),
    );
    await page.goto('./');

    const runs = page.getByRole('region', { name: 'Runs' }).locator('ul > li');
    await expect(runs).toHaveCount(2);
    await expect(runs.nth(1).getByRole('button', { name: 'Share' })).toHaveCount(0);

    await runs.nth(0).getByRole('button', { name: 'Share' }).click();
    const card = runs.nth(0).getByRole('region', { name: 'Share card' });
    await expect(card).toBeVisible();

    const img = card.getByRole('img', { name: 'Share card for Park Loop' });
    await expect.poll(() => img.evaluate((el: HTMLImageElement) => `${el.naturalWidth}x${el.naturalHeight}`)).toBe('1080x1920');

    const src = await img.getAttribute('src');
    expect(src).toMatch(/^data:image\/png;base64,/);
    mkdirSync('e2e/screenshots', { recursive: true });
    writeFileSync('e2e/screenshots/share-card.png', Buffer.from(src!.split(',')[1], 'base64'));

    // Saving falls back to a PNG download where the browser cannot share files.
    const download = page.waitForEvent('download');
    await card.getByRole('button', { name: 'Save image' }).click();
    expect((await download).suggestedFilename()).toBe('tracks-share.png');
  });
});
