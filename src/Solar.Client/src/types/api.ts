export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
  [key: string]: unknown;
}

export interface ApiResponse<T> {
  data: T | null;
  error: ProblemDetails | null;
  success: boolean;
}
