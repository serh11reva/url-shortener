import type {
  AnalyticsResponse,
  CheckAliasAvailabilityResponse,
  CreateShortUrlResponse,
  ProblemDetails,
} from './types';

function userFacingMessage(problem: ProblemDetails): string {
  return problem.detail?.trim() || problem.title?.trim() || 'Request failed';
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | null> {
  const ct = response.headers.get('content-type') ?? '';
  if (!ct.includes('application/problem')) {
    return null;
  }
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(message: string, status: number, problem: ProblemDetails | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export async function createShortUrl(
  longUrl: string,
  alias: string | undefined,
  signal?: AbortSignal,
): Promise<CreateShortUrlResponse> {
  const body: { longUrl: string; alias?: string } = { longUrl };
  if (alias?.trim()) {
    body.alias = alias.trim();
  }

  const response = await fetch('/api/urls', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  });

  if (!response.ok) {
    const problem = await readProblemDetails(response);
    throw new ApiError(userFacingMessage(problem ?? {}), response.status, problem);
  }

  return (await response.json()) as CreateShortUrlResponse;
}

export async function getAnalytics(shortCode: string, signal?: AbortSignal): Promise<AnalyticsResponse | null> {
  const response = await fetch(`/api/urls/${encodeURIComponent(shortCode)}/stats`, { signal });

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    const problem = await readProblemDetails(response);
    throw new ApiError(userFacingMessage(problem ?? {}), response.status, problem);
  }

  return (await response.json()) as AnalyticsResponse;
}

export async function checkAliasAvailability(
  alias: string,
  signal?: AbortSignal,
): Promise<CheckAliasAvailabilityResponse> {
  const response = await fetch(
    `/api/aliases/${encodeURIComponent(alias)}/availability`,
    { signal },
  );

  if (response.status === 400) {
    const problem = await readProblemDetails(response);
    throw new ApiError(userFacingMessage(problem ?? {}), response.status, problem);
  }

  if (!response.ok) {
    const problem = await readProblemDetails(response);
    throw new ApiError(userFacingMessage(problem ?? {}), response.status, problem);
  }

  return (await response.json()) as CheckAliasAvailabilityResponse;
}
