import { useSearchParams } from 'react-router-dom';
import Hero from '../components/Hero';
import RouteMap from '../components/RouteMap';
import { Badge, Button } from '../components/ui';
import { apiHref } from '../api/emptyApi';
import { useGetMeQuery, useGetRunsQuery, useDisconnectSpotifyMutation, useSignOutMutation } from '../api/generatedApi';

// The API sends int64 values as number | string, so coerce before formatting.
const formatDuration = (seconds: number | string) => {
  const total = Math.round(Number(seconds));
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = String(total % 60).padStart(2, '0');
  return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${s}` : `${m}:${s}`;
};

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });

// "Run to: Abbey Road", or "Run to: an album" when Spotify would not name it, or the mixed case.
const contextLine = (type: string, name: string | null) => {
  if (type === 'mixed') return 'Run to: a mix of songs';
  if (name !== null) return `Run to: ${name}`;
  return `Run to: ${/^[aeiou]/.test(type) ? 'an' : 'a'} ${type}`;
};

// Real anchors (full-page navigation to the API), styled like the design-system Button.
const linkButton = 'ui-button ui-button--primary ui-button--lg';
const note = { color: 'var(--text-secondary)', maxWidth: '480px', margin: '16px auto 0', fontSize: '0.95rem' } as const;

// Only mounted when signed in, so a signed-out 401 from /api/runs is never fetched or shown.
const Runs = () => {
  const { data: runs, isLoading, isError } = useGetRunsQuery(undefined, { refetchOnMountOrArgChange: true });

  return (
    <section className="container" id="runs" aria-label="Runs" style={{ paddingTop: '80px' }}>
      <div className="glass" style={{ padding: '32px', maxWidth: '640px', margin: '0 auto' }}>
        <h2 style={{ marginBottom: '16px', fontSize: '1.25rem' }}>Runs</h2>
        {isLoading && <p style={{ color: 'var(--text-secondary)' }}>Loading runs...</p>}
        {isError && <p style={{ color: '#f87171' }}>Could not reach the API. Is the backend running?</p>}
        {runs?.length === 0 && (
          <p style={{ color: 'var(--text-secondary)' }}>
            No runs yet. A run will appear here after the next one finishes on Strava.
          </p>
        )}
        {runs !== undefined && runs.length > 0 && (
          <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {runs.map((run) => (
              <li key={String(run.activityId)} style={{ marginBottom: '24px' }}>
                <h3 style={{ fontSize: '1.05rem' }}>{run.name}</h3>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                  {formatDate(run.startedAt)} · {formatDuration(run.elapsedSeconds)}
                </p>
                {/* BEH-HIGH-2: what most of the songs came from; nothing for a run with no songs or one synced before this existed. */}
                {run.songs.length > 0 && run.contextType && (
                  <p style={{ fontSize: '0.9rem' }}>{contextLine(run.contextType, run.contextName)}</p>
                )}
                {/* BEH-MAP-1/2: the route with each song pinned where it started; none for a run without GPS (BEH-MAP-3). */}
                {(run.route ?? []).length > 1 && (
                  <RouteMap
                    route={run.route.map((point) => point.map(Number))}
                    songs={run.songs.map((song) => ({
                      name: song.name,
                      albumArtUrl: song.albumArtUrl,
                      routeIndex: song.routeIndex == null ? null : Number(song.routeIndex),
                    }))}
                  />
                )}
                {run.songs.length === 0 ? (
                  <p style={{ color: 'var(--text-secondary)' }}>No songs found for this run.</p>
                ) : (
                  <ol style={{ paddingLeft: '20px', marginTop: '8px' }}>
                    {run.songs.map((song) => (
                      <li key={`${song.playedAt}-${song.name}`}>
                        {/* BEH-HIGH-1: the minute into the run it started; one already playing at the start reads 0:00. */}
                        <span style={{ color: 'var(--text-secondary)', fontVariantNumeric: 'tabular-nums' }}>
                          {formatDuration(Math.max(0, Number(song.offsetSeconds)))}
                        </span>{' '}
                        {song.name} <span style={{ color: 'var(--text-secondary)' }}>· {song.artists.join(', ')}</span>
                        {/* BEH-HIGH-3 / BEH-HIGH-4: the song at the run's top speed, and the one with the fastest average pace. */}
                        {song.isTopSpeed && (
                          <Badge variant="accent" style={{ marginLeft: '8px' }}>⚡ top speed</Badge>
                        )}
                        {song.isFastestPace && (
                          <Badge variant="success" style={{ marginLeft: '8px' }}>fastest pace</Badge>
                        )}
                      </li>
                    ))}
                  </ol>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
};

const Account = ({ spotifyConnected, onChanged }: { spotifyConnected: boolean; onChanged: () => void }) => {
  const [signOut, { isLoading: signingOut }] = useSignOutMutation();
  const [disconnect, { isLoading: disconnecting }] = useDisconnectSpotifyMutation();

  return (
    <div style={{ display: 'flex', gap: '8px', justifyContent: 'center', margin: '24px 0' }}>
      {spotifyConnected && (
        <Button
          variant="ghost"
          size="sm"
          loading={disconnecting}
          onClick={() => void disconnect().unwrap().then(onChanged)}
        >
          Disconnect Spotify
        </Button>
      )}
      <Button variant="ghost" size="sm" loading={signingOut} onClick={() => void signOut().unwrap().then(onChanged)}>
        Sign out
      </Button>
    </div>
  );
};

const Home = () => {
  const { data, isLoading, error, refetch } = useGetMeQuery();
  // The Strava callback sends a runner here with ?error=full when Strava's athlete cap is hit.
  const full = useSearchParams()[0].get('error') === 'full';
  // RTK Query keeps the last good data after a failed refetch; a 401 after sign-out must win.
  const me = error ? undefined : data;
  const signedOut = error !== undefined && 'status' in error && error.status === 401;

  if (me) {
    return (
      <>
        <Hero>
          {!me.spotifyConnected && (
            <div>
              <a className={linkButton} href={apiHref('api/auth/spotify')}>
                <span className="ui-button__label">Connect Spotify</span>
              </a>
              <p style={note}>Songs can't be matched to your runs until Spotify is connected.</p>
            </div>
          )}
        </Hero>
        <Runs />
        <Account spotifyConnected={me.spotifyConnected} onChanged={refetch} />
      </>
    );
  }

  return (
    <Hero>
      {signedOut && (
        <div>
          {full && (
            <p role="alert" style={{ ...note, color: '#f87171', marginBottom: '16px' }}>
              Tracks is full for now. Please try again later.
            </p>
          )}
          <a className={linkButton} href={apiHref('api/auth/strava')}>
            <span className="ui-button__label">Sign in with Strava</span>
          </a>
          <p style={note}>Tracks reads your runs from Strava and your listening from Spotify. Read-only.</p>
        </div>
      )}
      {isLoading && <p style={note}>Loading...</p>}
      {!isLoading && !signedOut && (
        <p style={{ ...note, color: '#f87171' }}>Could not reach the API. Is the backend running?</p>
      )}
    </Hero>
  );
};

export default Home;
