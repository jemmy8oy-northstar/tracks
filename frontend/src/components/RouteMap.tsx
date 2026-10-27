import './RouteMap.css';

export interface MapSong {
  name: string;
  albumArtUrl: string | null;
  /** The route point its pin sits on (BEH-MAP-2), or null. */
  routeIndex: number | null;
}

const WIDTH = 360;
const HEIGHT = 240;
const PADDING = 24;
const COVER = 26;

/** Fits [lat, lng] points into the box: equirectangular, squashed by latitude so a run keeps its shape. */
const project = (route: number[][]) => {
  const squash = Math.cos((route.reduce((sum, [lat]) => sum + lat, 0) / route.length) * (Math.PI / 180));
  const xs = route.map(([, lng]) => lng * squash);
  const ys = route.map(([lat]) => -lat);
  const [minX, maxX, minY, maxY] = [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
  const spanX = maxX - minX || 1e-9;
  const spanY = maxY - minY || 1e-9;
  const scale = Math.min((WIDTH - 2 * PADDING) / spanX, (HEIGHT - 2 * PADDING) / spanY);
  const offsetX = (WIDTH - spanX * scale) / 2;
  const offsetY = (HEIGHT - spanY * scale) / 2;
  return xs.map((x, i) => ({ x: offsetX + (x - minX) * scale, y: offsetY + (ys[i] - minY) * scale }));
};

/**
 * BEH-MAP-1: the run's route as an inline SVG (no map tiles), with each song's cover pinned where it
 * started, numbered to match the list below. A run without GPS never renders this (BEH-MAP-3).
 */
const RouteMap = ({ route, songs }: { route: number[][]; songs: MapSong[] }) => {
  const points = project(route);
  const line = points.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
  const pins = songs
    .map((song, i) => ({ song, number: i + 1, at: song.routeIndex === null ? undefined : points[song.routeIndex] }))
    .filter((pin) => pin.at !== undefined);

  return (
    <figure className="route-map" data-testid="route-map">
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} role="img" aria-label={`Route with ${pins.length} songs pinned`}>
        <polyline className="route-map__line" points={line} />
        <circle className="route-map__start" cx={points[0].x} cy={points[0].y} r={5} />
        {pins.map(({ song, number, at }) => (
          <g
            key={number}
            className="route-map__pin"
            data-testid="song-pin"
            transform={`translate(${at!.x - COVER / 2} ${at!.y - COVER / 2})`}
          >
            <title>{`${number}. ${song.name}`}</title>
            <rect width={COVER} height={COVER} rx={5} />
            {song.albumArtUrl && (
              <image href={song.albumArtUrl} width={COVER} height={COVER} style={{ clipPath: 'inset(0 round 5px)' }} />
            )}
            <text x={COVER} y={2} textAnchor="middle">
              {number}
            </text>
          </g>
        ))}
      </svg>
    </figure>
  );
};

export default RouteMap;
