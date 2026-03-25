import type {
  AnalyticsResponse,
  CheckAliasAvailabilityResponse,
  CreateShortUrlResponse,
  ProblemDetails,
} from './types';

function userFacingMessage(problem: ProblemDetails, httpStatus: number): string {
  const detail = problem.detail?.trim();
  const title = problem.title?.trim();
  const fromBody = detail || title;
  if (fromBody) {
    return fromBody;
  }
  if (httpStatus === 409) {
    return 'This alias is already in use.';
  }
  return 'Request failed';
}

async function readProblemDetails(response: Response): Promise<ProblemDetails | null> {
  const ct = response.headers.get('content-type') ?? '';
  const looksLikeJson =
    ct.includes('application/json') || ct.includes('application/problem');
  if (!looksLikeJson) {
    return null;
  }
  try {
    const raw = (await response.json()) as Record<string, unknown>;
    return {
      type: pickString(raw, 'type', 'Type'),
      title: pickString(raw, 'title', 'Title'),
      detail: pickString(raw, 'detail', 'Detail'),
      status: pickNumber(raw, 'status', 'Status'),
      instance: pickString(raw, 'instance', 'Instance'),
    };
  } catch {
    return null;
  }
}

function pickString(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
): string | undefined {
  const a = raw[camel];
  const b = raw[pascal];
  if (typeof a === 'string' && a.length > 0) {
    return a;
  }
  if (typeof b === 'string' && b.length > 0) {
    return b;
  }
  return undefined;
}

function pickNumber(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
): number | undefined {
  const a = raw[camel];
  const b = raw[pascal];
  if (typeof a === 'number' && Number.isFinite(a)) {
    return a;
  }
  if (typeof b === 'number' && Number.isFinite(b)) {
    return b;
  }
  return undefined;
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
    throw new ApiError(userFacingMessage(problem ?? {}, response.status), response.status, problem);
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
    throw new ApiError(userFacingMessage(problem ?? {}, response.status), response.status, problem);
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
    throw new ApiError(userFacingMessage(problem ?? {}, response.status), response.status, problem);
  }

  if (!response.ok) {
    const problem = await readProblemDetails(response);
    throw new ApiError(userFacingMessage(problem ?? {}, response.status), response.status, problem);
  }

  return (await response.json()) as CheckAliasAvailabilityResponse;
}
