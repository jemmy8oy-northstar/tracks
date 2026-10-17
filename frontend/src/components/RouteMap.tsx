import React from 'react';
import { MAP_HEIGHT, MAP_WIDTH, type WorkoutView } from '../lib/musicMapView';
import './RouteMap.css';

const COVER = 24;

/**
 * BEH-MAP-1, region RouteMap: the route as an inline SVG polyline (no map tiles) with each song
 * pinned where it started, shown as a cover square numbered to match the song list.
 */
const RouteMap: React.FC<{ workout: WorkoutView }> = ({ workout }) => {
  const line = workout.route.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
  const start = workout.route[0];
  const pinned = workout.songs.filter((s) => s.pin !== null);

  return (
    <section className="route-map glass" aria-label="Route map" data-testid="route-map">
      <svg
        viewBox={`0 0 ${MAP_WIDTH} ${MAP_HEIGHT}`}
        role="img"
        aria-label={`Route with ${pinned.length} songs pinned`}
      >
        <polyline className="route-map__line" points={line} />
        <circle className="route-map__start" cx={start.x} cy={start.y} r={5} />
        {pinned.map((song) => (
          <g
            key={song.index}
            className="route-map__pin"
            data-testid="song-pin"
            transform={`translate(${song.pin!.x - COVER / 2} ${song.pin!.y - COVER / 2})`}
          >
            <title>{`${song.index}. ${song.title} - ${song.artist}`}</title>
            <rect width={COVER} height={COVER} rx={5} fill={song.coverColour} />
            <text x={COVER / 2} y={COVER / 2} dy="0.35em" textAnchor="middle">
              {song.index}
            </text>
          </g>
        ))}
      </svg>
    </section>
  );
};

export default RouteMap;
