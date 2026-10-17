import { emptySplitApi as api } from "./emptyApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getStatus: build.query<GetStatusApiResponse, GetStatusApiArg>({
      query: () => ({ url: `/api/status` }),
    }),
    verifyStravaWebhook: build.query<
      VerifyStravaWebhookApiResponse,
      VerifyStravaWebhookApiArg
    >({
      query: (queryArg) => ({
        url: `/api/strava/webhook`,
        params: {
          "hub.mode": queryArg["hub.mode"],
          "hub.verify_token": queryArg["hub.verify_token"],
          "hub.challenge": queryArg["hub.challenge"],
        },
      }),
    }),
    receiveStravaWebhookEvent: build.mutation<
      ReceiveStravaWebhookEventApiResponse,
      ReceiveStravaWebhookEventApiArg
    >({
      query: (queryArg) => ({
        url: `/api/strava/webhook`,
        method: "POST",
        body: queryArg.stravaWebhookEvent,
      }),
    }),
    getRuns: build.query<GetRunsApiResponse, GetRunsApiArg>({
      query: () => ({ url: `/api/runs` }),
    }),
  }),
  overrideExisting: false,
});
export { injectedRtkApi as enhancedApi };
export type GetStatusApiResponse = unknown;
export type GetStatusApiArg = void;
export type VerifyStravaWebhookApiResponse = /** status 200 OK */ {
  [key: string]: string;
};
export type VerifyStravaWebhookApiArg = {
  "hub.mode"?: string;
  "hub.verify_token"?: string;
  "hub.challenge"?: string;
};
export type ReceiveStravaWebhookEventApiResponse = unknown;
export type ReceiveStravaWebhookEventApiArg = {
  stravaWebhookEvent: StravaWebhookEvent;
};
export type GetRunsApiResponse = /** status 200 OK */ RunResponse[];
export type GetRunsApiArg = void;
export type StravaWebhookEvent = {
  object_type?: null | string;
  object_id?: number | string;
  aspect_type?: null | string;
  owner_id?: number | string;
};
export type RunSongResponse = {
  name: string;
  artists: string[];
  albumArtUrl: null | string;
  playedAt: string;
  offsetSeconds: number | string;
};
export type RunResponse = {
  activityId: number | string;
  name: string;
  sportType: string;
  startedAt: string;
  elapsedSeconds: number | string;
  songs: RunSongResponse[];
};
export const {
  useGetStatusQuery,
  useVerifyStravaWebhookQuery,
  useReceiveStravaWebhookEventMutation,
  useGetRunsQuery,
} = injectedRtkApi;
