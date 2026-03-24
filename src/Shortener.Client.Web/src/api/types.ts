export interface CreateShortUrlResponse {
  shortCode: string;
}

export interface AnalyticsResponse {
  clickCount: number;
  lastAccessed: string | null;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  instance?: string;
}

export interface CheckAliasAvailabilityResponse {
  available: boolean;
}
