import type { ProblemDetails } from "./types";

/** Typed error thrown by the API client for any non-2xx response. */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string | undefined;
  readonly errors?: Record<string, string[]> | undefined;

  constructor(status: number, title: string, problem?: ProblemDetails) {
    super(problem?.detail || title);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.detail = problem?.detail ?? undefined;
    this.errors = problem?.errors;
  }

  get isNotFound() {
    return this.status === 404;
  }

  get isValidation() {
    return this.status === 400 && !!this.errors;
  }
}
