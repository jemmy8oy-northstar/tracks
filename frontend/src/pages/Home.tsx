import { useSearchParams } from 'react-router-dom';
import Hero from '../components/Hero';
import { Button } from '../components/ui';
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
                {run.songs.length === 0 ? (
                  <p style={{ color: 'var(--text-secondary)' }}>No songs found for this run.</p>
                ) : (
                  <ol style={{ paddingLeft: '20px', marginTop: '8px' }}>
                    {run.songs.map((song) => (
                      <li key={`${song.playedAt}-${song.name}`}>
                        {song.name} <span style={{ color: 'var(--text-secondary)' }}>· {song.artists.join(', ')}</span>
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
