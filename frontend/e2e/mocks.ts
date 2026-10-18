import type { Page } from '@playwright/test';

/** Who /api/me says is signed in. `null` = signed out (401). */
export type Me = { athleteId: number; spotifyConnected: boolean } | null;

/**
 * Deterministic API mocks for the e2e suite.
 *
 * Every backend call the frontend makes is fulfilled here with a fixed fixture,
 * so the tests never touch a real server and render identically every run.
 * Add a new route here whenever the frontend starts calling a new endpoint.
 */
export async function mockApi(page: Page, me: Me = { athleteId: 1, spotifyConnected: true }): Promise<void> {
  await page.route('**/api/status', (route) =>
    route.fulfill({ json: { status: 'Healthy', service: 'Balenthiran.Tracks.WebApi' } }),
  );
  // Default to a signed-in runner with Spotify connected; specs override via mockMe.
  await mockMe(page, me);
  // Default to no runs; specs that need runs register their own route after this.
  await page.route('**/api/runs', (route) => route.fulfill({ json: [] }));
  // Interest / newsletter forms — accept any submission so the UI shows success.
  await page.route('**/api/interest/**', (route) =>
    route.fulfill({ status: 200, json: { ok: true } }),
  );
}

/** Registered after mockApi's default, this wins. */
export async function mockMe(page: Page, me: Me): Promise<void> {
  await page.route('**/api/me', (route) =>
    me ? route.fulfill({ json: me }) : route.fulfill({ status: 401, json: { title: 'Unauthorized' } }),
  );
}
