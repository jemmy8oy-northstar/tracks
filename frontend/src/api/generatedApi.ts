import { emptySplitApi as api } from "./emptyApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getStatus: build.query<GetStatusApiResponse, GetStatusApiArg>({
      query: () => ({ url: `/api/status` }),
    }),
    listWorkouts: build.query<ListWorkoutsApiResponse, ListWorkoutsApiArg>({
      query: () => ({ url: `/api/workouts` }),
    }),
    getWorkoutMap: build.query<GetWorkoutMapApiResponse, GetWorkoutMapApiArg>({
      query: (queryArg) => ({ url: `/api/workouts/${queryArg.id}` }),
    }),
  }),
  overrideExisting: false,
});
export { injectedRtkApi as enhancedApi };
export type GetStatusApiResponse = unknown;
export type GetStatusApiArg = void;
export type ListWorkoutsApiResponse = /** status 200 OK */ IWorkoutSummary[];
export type ListWorkoutsApiArg = void;
export type GetWorkoutMapApiResponse = /** status 200 OK */ IMusicMap;
export type GetWorkoutMapApiArg = {
  id: string;
};
export type IWorkoutSummary = {
  id?: string;
  name?: string;
  startedAt?: string;
  hasRoute?: boolean;
};
export type IRoutePoint = {
  latitude?: number | string;
  longitude?: number | string;
  time?: string;
};
export type ISongPin = {
  title?: string;
  artist?: string;
  coverColour?: string;
  startedAt?: string;
  elapsedSeconds?: number | string;
  distanceMetres?: null | number | string;
  pin?: null | IRoutePoint;
};
export type IMusicMap = {
  id?: string;
  name?: string;
  startedAt?: string;
  endedAt?: string;
  hasRoute?: boolean;
  route?: IRoutePoint[];
  songs?: ISongPin[];
};
export const {
  useGetStatusQuery,
  useListWorkoutsQuery,
  useGetWorkoutMapQuery,
} = injectedRtkApi;
