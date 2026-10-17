import Hero from '../components/Hero';
import { useGetRunsQuery } from '../api/generatedApi';

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

const Home = () => {
  const { data: runs, isLoading, isError } = useGetRunsQuery();

  return (
    <>
      <Hero />
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
    </>
  );
};

export default Home;
