import { emptySplitApi } from './emptyApi';

// Hand-written because POST /api/auth/signout is not in the generated OpenAPI client.
export const authApi = emptySplitApi.injectEndpoints({
  endpoints: (build) => ({
    signOut: build.mutation<void, void>({
      query: () => ({ url: '/api/auth/signout', method: 'POST' }),
    }),
  }),
});

export const { useSignOutMutation } = authApi;
