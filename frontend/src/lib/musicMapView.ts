import type { IMusicMap } from '../api/generatedApi';

/**
 * The generated API types mark every field optional and allow numbers as strings
 * (that is what the OpenAPI schema says). The view works on this strict shape instead,
 * built once here.
 */
export interface RouteXY {
  x: number;
  y: number;
}

export interface SongView {
  index: number;
  title: string;
  artist: string;
  coverColour: string;
  elapsedSeconds: number;
  distanceMetres: number | null;
  /** Position of the pin on the map, or null when the workout has no route. */
  pin: RouteXY | null;
}

export interface WorkoutView {
  id: string;
  name: string;
  startedAt: Date;
  hasRoute: boolean;
  route: RouteXY[];
  songs: SongView[];
}

export const MAP_WIDTH = 360;
export const MAP_HEIGHT = 300;
const MAP_PADDING = 36;

const num = (value: number | string | null | undefined): number => Number(value ?? 0);

/** Fits lat/lon points into the map box (equirectangular, corrected for latitude). */
function projector(points: { lat: number; lon: number }[]) {
  const meanLat = points.reduce((sum, p) => sum + p.lat, 0) / points.length;
  const squash = Math.cos((meanLat * Math.PI) / 180);
  const xs = points.map((p) => p.lon * squash);
  const ys = points.map((p) => -p.lat);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);
  const spanX = maxX - minX || 1e-9;
  const spanY = maxY - minY || 1e-9;
  const scale = Math.min(
    (MAP_WIDTH - 2 * MAP_PADDING) / spanX,
    (MAP_HEIGHT - 2 * MAP_PADDING) / spanY,
  );
  const offsetX = (MAP_WIDTH - spanX * scale) / 2;
  const offsetY = (MAP_HEIGHT - spanY * scale) / 2;

  return (lat: number, lon: number): RouteXY => ({
    x: offsetX + (lon * squash - minX) * scale,
    y: offsetY + (-lat - minY) * scale,
  });
}

export function toWorkoutView(map: IMusicMap): WorkoutView {
  const route = (map.route ?? []).map((p) => ({ lat: num(p.latitude), lon: num(p.longitude) }));
  const project = route.length > 0 ? projector(route) : null;

  return {
    id: map.id ?? '',
    name: map.name ?? '',
    startedAt: new Date(map.startedAt ?? 0),
    hasRoute: route.length > 0,
    route: project ? route.map((p) => project(p.lat, p.lon)) : [],
    songs: (map.songs ?? []).map((s, i) => ({
      index: i + 1,
      title: s.title ?? '',
      artist: s.artist ?? '',
      coverColour: s.coverColour ?? '',
      elapsedSeconds: num(s.elapsedSeconds),
      distanceMetres: s.distanceMetres == null ? null : num(s.distanceMetres),
      pin: project && s.pin ? project(num(s.pin.latitude), num(s.pin.longitude)) : null,
    })),
  };
}

export function formatDistance(metres: number): string {
  return metres < 1000 ? `${Math.round(metres)} m` : `${(metres / 1000).toFixed(2)} km`;
}

export function formatElapsed(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${String(s).padStart(2, '0')}`;
}
