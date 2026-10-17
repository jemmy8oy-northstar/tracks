import React from 'react';
import { formatDistance, formatElapsed, type WorkoutView } from '../lib/musicMapView';
import './SongList.css';

/**
 * BEH-MAP-1, region SongList: the same songs in order, with the distance each one covered.
 * BEH-MAP-3: a workout without GPS has no distances, so the list shows elapsed time instead.
 */
const SongList: React.FC<{ workout: WorkoutView }> = ({ workout }) => {
  if (workout.songs.length === 0) {
    return (
      <section className="song-list glass" aria-label="Songs" data-testid="song-list">
        <p className="song-list__empty">No songs played during this workout.</p>
      </section>
    );
  }

  return (
    <section className="song-list glass" aria-label="Songs" data-testid="song-list">
      <ol>
        {workout.songs.map((song) => (
          <li key={song.index} className="song-list__row" data-testid="song-row">
            <span className="song-list__cover" style={{ background: song.coverColour }}>
              {song.index}
            </span>
            <span className="song-list__text">
              <span className="song-list__title">{song.title}</span>
              <span className="song-list__artist">{song.artist}</span>
            </span>
            <span className="song-list__meta">
              {song.distanceMetres !== null && (
                <span className="song-list__distance">{formatDistance(song.distanceMetres)}</span>
              )}
              <span className="song-list__elapsed">at {formatElapsed(song.elapsedSeconds)}</span>
            </span>
          </li>
        ))}
      </ol>
    </section>
  );
};

export default SongList;
