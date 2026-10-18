import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

// The app is served under a prefix (vite `base`), so the API is too: `/tracks/api/...`.
const baseUrl = import.meta.env.BASE_URL;

/** URL for a full-page navigation to the API (e.g. an OAuth redirect), built like the client's own requests. */
export const apiHref = (path: string) => `${baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;

// initialize an empty api service that we'll inject endpoints into later as needed
export const emptySplitApi = createApi({
  baseQuery: fetchBaseQuery({ baseUrl }),
  endpoints: () => ({}),
});
