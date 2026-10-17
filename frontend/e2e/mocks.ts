import type { Page } from '@playwright/test';

/**
 * Deterministic API mocks for the e2e suite.
 *
 * Every backend call the frontend makes is fulfilled here with a fixed fixture,
 * so the tests never touch a real server and render identically every run.
 * Add a new route here whenever the frontend starts calling a new endpoint.
 */
const runStart = Date.UTC(2026, 9, 10, 8, 0, 0);

/** An outdoor workout: 1.25 laps of a wide ellipse, a fix every 30 s, 5 songs with distances. */
const outdoorWorkout = {
  id: 'park-run',
  name: 'Morning loop of Elm Park',
  startedAt: new Date(runStart).toISOString(),
  endedAt: new Date(runStart + 1_800_000).toISOString(),
  hasRoute: true,
  route: Array.from({ length: 61 }, (_, i) => {
    const angle = (1.25 * 2 * Math.PI * i) / 60;
    return {
      latitude: 51.5 + 0.003 * Math.sin(angle),
      longitude: -0.15 + 0.011 * Math.cos(angle),
      time: new Date(runStart + i * 30_000).toISOString(),
    };
  }),
  songs: [
    ['Tempo Lines', 'The Stride Club', '#e11d48', 0, 0, 428],
    ['Park Lights', 'Marlow & Finch', '#f59e0b', 215, 7, 535],
    ['Second Wind', 'Nova Lane', '#10b981', 480, 16, 712],
    ['Gravel Hymn', 'Old Rowan', '#6366f1', 700, 23, 468],
    ['Mile Marker', 'Hollow Hours', '#0ea5e9', 960, 32, 1_210],
  ].map(([title, artist, coverColour, elapsed, fix, distance]) => {
    const angle = (1.25 * 2 * Math.PI * (fix as number)) / 60;
    return {
      title,
      artist,
      coverColour,
      startedAt: new Date(runStart + (elapsed as number) * 1000).toISOString(),
      elapsedSeconds: elapsed,
      distanceMetres: distance,
      pin: {
        latitude: 51.5 + 0.003 * Math.sin(angle),
        longitude: -0.15 + 0.011 * Math.cos(angle),
        time: new Date(runStart + (fix as number) * 30_000).toISOString(),
      },
    };
  }),
};

/** An indoor workout: songs only, no GPS. */
const indoorWorkout = {
  id: 'indoor-ride',
  name: 'Indoor ride',
  startedAt: new Date(Date.UTC(2026, 9, 11, 18, 0, 0)).toISOString(),
  endedAt: new Date(Date.UTC(2026, 9, 11, 18, 45, 0)).toISOString(),
  hasRoute: false,
  route: [],
  songs: [
    ['Spin Cycle', 'Gear & Gale', '#f97316', 0],
    ['Cadence', 'Nova Lane', '#22c55e', 240],
    ['Saddle Sore', 'Old Rowan', '#ef4444', 540],
  ].map(([title, artist, coverColour, elapsed]) => ({
    title,
    artist,
    coverColour,
    startedAt: new Date(Date.UTC(2026, 9, 11, 18, 0, 0) + (elapsed as number) * 1000).toISOString(),
    elapsedSeconds: elapsed,
    distanceMetres: null,
    pin: null,
  })),
};

export async function mockApi(page: Page): Promise<void> {
  await page.route('**/api/workouts', (route) =>
    route.fulfill({
      json: [outdoorWorkout, indoorWorkout].map((w) => ({
        id: w.id,
        name: w.name,
        startedAt: w.startedAt,
        hasRoute: w.hasRoute,
      })),
    }),
  );
  await page.route('**/api/workouts/park-run', (route) => route.fulfill({ json: outdoorWorkout }));
  await page.route('**/api/workouts/indoor-ride', (route) => route.fulfill({ json: indoorWorkout }));
  await page.route('**/api/status', (route) =>
    route.fulfill({ json: { status: 'Healthy', service: 'Balenthiran.Tracks.WebApi' } }),
  );
  // Interest / newsletter forms — accept any submission so the UI shows success.
  await page.route('**/api/interest/**', (route) =>
    route.fulfill({ status: 200, json: { ok: true } }),
  );
}
