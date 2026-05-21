import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { loadRuntimeConfig } from './hooks/useRuntimeConfig';
import { App } from './App';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) => {
        const status = (error as { status?: number }).status;
        // Don't retry 4xx errors (client errors won't self-resolve)
        if (status && status >= 400 && status < 500) return false;
        // Retry up to 3 times for network/5xx errors
        return failureCount < 3;
      },
    },
    mutations: {
      retry: 0,
    },
  },
});

async function bootstrap() {
  await loadRuntimeConfig();

  ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </React.StrictMode>
  );
}

bootstrap();
