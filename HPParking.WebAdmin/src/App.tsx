import { useEffect, useMemo } from 'react';
import { RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { router } from '@/routes';
import { authApi } from '@/api/authApi';
import { useAuthStore } from '@/stores/authStore';

export default function App() {
  const queryClient = useMemo(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            retry: 1,
            refetchOnWindowFocus: false,
          },
        },
      }),
    []
  );

  useEffect(() => {
    // Initial silent check of existing HttpOnly cookie session
    authApi
      .getCurrentUser()
      .then((user) => {
        useAuthStore.getState().setAuth(user);
      })
      .catch(() => {
        useAuthStore.getState().clearAuth();
      });
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <Toaster position="top-right" richColors closeButton />
    </QueryClientProvider>
  );
}
