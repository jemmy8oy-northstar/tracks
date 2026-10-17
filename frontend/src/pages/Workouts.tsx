import { Link } from 'react-router-dom';
import { useListWorkoutsQuery } from '../api/generatedApi';
import './Workout.css';

const Workouts = () => {
  const { data, isLoading, isError } = useListWorkoutsQuery();

  return (
    <section className="container workout-page">
      <h1>Workouts</h1>
      {isLoading && <p className="workout-page__note">Loading workouts...</p>}
      {isError && <p className="workout-page__note">Could not load workouts.</p>}
      <ul className="workout-list">
        {(data ?? []).map((w) => (
          <li key={w.id}>
            <Link to={`/workouts/${w.id}`} className="glass glass-hover">
              <span className="workout-list__name">{w.name}</span>
              <span className="workout-list__meta">
                {new Date(w.startedAt ?? 0).toLocaleDateString('en-GB', { dateStyle: 'medium', timeZone: 'UTC' })}
                {w.hasRoute ? ' · route + songs' : ' · songs only (no GPS)'}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
};

export default Workouts;
