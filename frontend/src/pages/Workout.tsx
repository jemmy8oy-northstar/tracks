import { Link, useParams } from 'react-router-dom';
import { useGetWorkoutMapQuery } from '../api/generatedApi';
import RouteMap from '../components/RouteMap';
import SongList from '../components/SongList';
import { toWorkoutView } from '../lib/musicMapView';
import './Workout.css';

/** BEH-MAP-1: each workout shows the songs played during it, pinned along its route. */
const Workout = () => {
  const { id = '' } = useParams();
  const { data, isLoading, isError } = useGetWorkoutMapQuery({ id });

  if (isLoading) return <section className="container workout-page"><p className="workout-page__note">Loading workout...</p></section>;
  if (isError || !data) {
    return (
      <section className="container workout-page">
        <p className="workout-page__note">Could not load this workout.</p>
        <Link to="/workouts" className="workout-page__back">All workouts</Link>
      </section>
    );
  }

  const workout = toWorkoutView(data);

  return (
    <section className="container workout-page">
      <Link to="/workouts" className="workout-page__back">All workouts</Link>
      <h1>{workout.name}</h1>
      <p className="workout-page__sub">
        {workout.startedAt.toLocaleDateString('en-GB', { dateStyle: 'medium', timeZone: 'UTC' })}
        {' · '}
        {workout.songs.length} {workout.songs.length === 1 ? 'song' : 'songs'}
        {!workout.hasRoute && ' · no GPS'}
      </p>
      {workout.hasRoute && <RouteMap workout={workout} />}
      <SongList workout={workout} />
    </section>
  );
};

export default Workout;
