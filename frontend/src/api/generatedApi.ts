import { emptySplitApi as api } from "./emptyApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getStatus: build.query<GetStatusApiResponse, GetStatusApiArg>({
      query: () => ({ url: `/api/status` }),
    }),
    startStravaSignIn: build.query<
      StartStravaSignInApiResponse,
      StartStravaSignInApiArg
    >({
      query: () => ({ url: `/api/auth/strava` }),
    }),
    finishStravaSignIn: build.query<
      FinishStravaSignInApiResponse,
      FinishStravaSignInApiArg
    >({
      query: (queryArg) => ({
        url: `/api/auth/strava/callback`,
        params: {
          code: queryArg.code,
          state: queryArg.state,
          scope: queryArg.scope,
          error: queryArg.error,
        },
      }),
    }),
    signOut: build.mutation<SignOutApiResponse, SignOutApiArg>({
      query: () => ({ url: `/api/auth/signout`, method: "POST" }),
    }),
    startSpotifyConnect: build.query<
      StartSpotifyConnectApiResponse,
      StartSpotifyConnectApiArg
    >({
      query: () => ({ url: `/api/auth/spotify` }),
    }),
    disconnectSpotify: build.mutation<
      DisconnectSpotifyApiResponse,
      DisconnectSpotifyApiArg
    >({
      query: () => ({ url: `/api/auth/spotify`, method: "DELETE" }),
    }),
    finishSpotifyConnect: build.query<
      FinishSpotifyConnectApiResponse,
      FinishSpotifyConnectApiArg
    >({
      query: (queryArg) => ({
        url: `/api/auth/spotify/callback`,
        params: {
          code: queryArg.code,
          state: queryArg.state,
          error: queryArg.error,
        },
      }),
    }),
    getMe: build.query<GetMeApiResponse, GetMeApiArg>({
      query: () => ({ url: `/api/me` }),
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
export type StartStravaSignInApiResponse = unknown;
export type StartStravaSignInApiArg = void;
export type FinishStravaSignInApiResponse = unknown;
export type FinishStravaSignInApiArg = {
  code?: string;
  state?: string;
  scope?: string;
  error?: string;
};
export type SignOutApiResponse = unknown;
export type SignOutApiArg = void;
export type StartSpotifyConnectApiResponse = unknown;
export type StartSpotifyConnectApiArg = void;
export type DisconnectSpotifyApiResponse = unknown;
export type DisconnectSpotifyApiArg = void;
export type FinishSpotifyConnectApiResponse = unknown;
export type FinishSpotifyConnectApiArg = {
  code?: string;
  state?: string;
  error?: string;
};
export type GetMeApiResponse = /** status 200 OK */ MeResponse;
export type GetMeApiArg = void;
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
export type MeResponse = {
  athleteId: number | string;
  spotifyConnected: boolean;
};
export type StravaWebhookEvent = {
  object_type?: null | string;
  object_id?: number | string;
  aspect_type?: null | string;
  owner_id?: number | string;
  updates?: null | object;
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
  useStartStravaSignInQuery,
  useFinishStravaSignInQuery,
  useSignOutMutation,
  useStartSpotifyConnectQuery,
  useDisconnectSpotifyMutation,
  useFinishSpotifyConnectQuery,
  useGetMeQuery,
  useVerifyStravaWebhookQuery,
  useReceiveStravaWebhookEventMutation,
  useGetRunsQuery,
} = injectedRtkApi;
