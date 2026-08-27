import { useState, useEffect, useCallback } from 'react';
import type { ProblemDetails } from '../types/api';
import { ApiError } from '../services/apiClient';

interface UseApiResult<T> {
  data: T | null;
  loading: boolean;
  error: ProblemDetails | string | null;
  refetch: () => Promise<void>;
  setData: React.Dispatch<React.SetStateAction<T | null>>;
}

export function useApi<T>(
  fetcher: () => Promise<T>,
  dependencies: unknown[] = [],
  autoFetch: boolean = true
): UseApiResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState<boolean>(autoFetch);
  const [error, setError] = useState<ProblemDetails | string | null>(null);

  const execute = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await fetcher();
      setData(result);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.problem);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Ocorreu um erro desconhecido ao carregar os dados.');
      }
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencies);

  useEffect(() => {
    if (autoFetch) {
      execute();
    }
  }, [execute, autoFetch]);

  return {
    data,
    loading,
    error,
    refetch: execute,
    setData,
  };
}
