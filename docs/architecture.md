# Architecture and technical decisions

## 1. The system

A back-office tool for a tour operator to manage **suppliers** (lodges, hotels, activity operators, transfer companies, restaurants) and the **services** they sell, with prices. It also stores **photos and videos** for each supplier and uses **AI to read a pasted rate sheet or email** and pre-fill the supplier form.

- **`backend/`**: the Supplier API, ASP.NET Core (.NET 10) with its own SQL Server database.
- **`frontend/`**: the Supplier Hub web app, a React single-page app (started in Lovable).

## 2. Architecture overview

```
Browser (React SPA)
   │  JSON over HTTP (/api/v1)          │  file bytes (signed URLs)
   ▼                                     ▼
Supplier API (.NET 10) ───────────▶ S3 bucket (AWS in production; Versity S3 Gateway locally)
   │   ├─ SQL Server (own "supplier" schema)
   │   └─ AI model (Gemini via an OpenAI-compatible API), optional
```

The backend follows **Clean Architecture with ports and adapters**. Dependencies point inward:

| Layer | Contains | Depends on |
| --- | --- | --- |
| **Domain** | `Supplier` aggregate, `Service`, `SupplierMedia`, enums, business rules | nothing (no NuGet packages) |
| **Application** | use cases (handlers), validation, DTOs, **ports**: `ISupplierRepository`, `ISupplierQueries`, `IMediaStorage`, `ISupplierExtractionService` | Domain |
| **Infrastructure** | EF Core + SQL Server, S3 adapter, AI adapter; implements the ports | Application |
| **Api** | minimal-API endpoints, error handling, OpenAPI, health, CORS, rate limiting, logging | Infrastructure (to wire everything up at startup) |

**Why:** business rules are testable without a database or web server, and the database, file storage and AI are replaceable. This paid off when media storage moved from local disk to S3: only a new adapter and new endpoints were needed, and the domain and database stayed the same.

## 3. Domain model and business rules

- **`Supplier` is the aggregate root.** Services and media can only be added through it (`AddService`, `AddMedia`), so its rules can't be bypassed. Collections are private and exposed read-only.
- **Rules**, enforced as guard clauses that throw a `DomainException`:
  - names are required and at most 200 characters
  - prices are 0 or more
  - currency is a 3-letter uppercase ISO code
  - duration and capacity must be positive when given
  - at most 50 services and 20 media files per supplier
  - photos are at most 10 MB and videos at most 100 MB
- **Ids are GUID v7** (time-ordered), so SQL Server indexes don't fragment the way they do with random GUIDs, and ids can be created before saving.
- **Enums travel and are stored as strings**, so data is readable and adding a new value never changes the meaning of existing rows.
- **Money is `decimal(18,2)`**, never a floating-point number.

## 4. Application layer

- **No MediatR and no AutoMapper.** Both became commercial in 2025. Plain handler classes and hand-written mappings are explicit and easy to debug.
- **Separate read and write paths.** Writes load the aggregate through `ISupplierRepository`, so the domain rules run. Reads use `ISupplierQueries`, which projects straight into DTOs without change tracking.
- **Validation twice, on purpose.** FluentValidation at the edge returns all field errors at once, with keys like `Services[0].Price` that the frontend maps onto form fields. The domain guards are the backstop: an invalid supplier can't exist even if validation were skipped.
- **Validation runs inside the create use case**, not in an HTTP filter, so it applies whatever calls it. The AI feature reuses the same validator to produce its warnings.
- **Missing values fail instead of defaulting.** Supplier type, price and pricing unit are nullable in the request, so leaving one out returns 400 rather than, say, silently creating an "Accommodation" supplier.

## 5. API design

- **Versioned REST under `/api/v1`**, using minimal APIs.

  | Method | Route | Purpose |
  | --- | --- | --- |
  | GET | `/suppliers` | list, with search (name, city or email), type filter and paging (page size capped at 50) |
  | GET | `/suppliers/{id}` | one supplier; 404 if missing |
  | POST | `/suppliers` | create: 201 with `Location`, 400 on validation errors, 409 on duplicates |
  | POST | `/suppliers/extract` | AI draft |
  | POST | `/suppliers/{id}/media/uploads` | get a signed upload URL |
  | POST | `/suppliers/{id}/media` | confirm an uploaded file |
  | DELETE | `/suppliers/{id}/media/{mediaId}` | remove a photo or video |
  | GET | `/media/{id}` | view a photo or video (redirects to S3) |
  | GET | `/features` | which optional features are on |
  | GET | `/health/live`, `/health/ready` | health checks |

- **One error format, RFC 7807 ProblemDetails**, from a global handler:

  | Status | Cause |
  | --- | --- |
  | 400 | validation or domain rule broken |
  | 404 | not found |
  | 409 | conflict |
  | 429 | rate limit |
  | 502 | AI failure |
  | 503 | feature switched off |
  | 500 | anything else; no internal details are sent to the client |

  Expected errors (4xx) aren't logged as failures, which keeps the logs meaningful.
- **The OpenAPI contract is the source of truth.** Scalar serves interactive docs, and the frontend generates its TypeScript types from it. Numbers must be sent as JSON numbers, so the contract says `number`, not `number | string`. Descriptions are formatted with the invariant culture, so the server's language settings can't change the contract.
- **Health checks** follow the Kubernetes model: *live* means the process is up (if it fails, the service is restarted); *ready* means SQL Server is also reachable (if it fails, traffic stops being sent to it).

## 6. Persistence

- **EF Core 10 on SQL Server**, in its own `supplier` schema, with migrations. They're applied on startup only when configured (development and compose).
- **Duplicates are blocked twice:** a friendly check before inserting, and a **unique index on (Name, City)**, so two simultaneous requests produce a 409 instead of a duplicate row.
- **Optimistic concurrency** with a `rowversion` column, ready for editing.
- **Retry on failure**, which covers SQL Server starting slowly in Docker.
- The repository translates database errors (unique-index violations, concurrency conflicts) into an application `ConflictException`, so the API never needs to know about EF Core or SQL Server.
- **Schema changes preserve data**, e.g. the address column was renamed with `RenameColumn` rather than dropped and re-added, and new columns get sensible defaults for existing rows.

## 7. Photos and videos (S3)

- **The database stores references, not files.** Each file's row holds its S3 key (e.g. `suppliers/{id}/{mediaId}.mp4`) plus type, size and name. The bytes live in S3, so the database stays small and fast.
- **Direct-to-S3 upload in three steps:**
  1. **Request an upload.** The API checks the declared type, size and per-supplier limit, then returns a signed PUT URL valid for 15 minutes.
  2. **Upload.** The browser sends the file straight to S3, showing progress.
  3. **Confirm.** The API checks the object exists, **reads its first bytes to identify the real file type** (a renamed executable is rejected and deleted), records its actual size, and saves the reference. Confirming twice returns the same result, so retries are safe.
- **Viewing:** `/media/{id}` is a stable URL that redirects to a signed S3 GET URL valid for one hour. S3 handles range requests, so videos can be streamed and seeked.
- **Why:** 100 MB videos never pass through the API's memory, bandwidth or request limits. S3 handles the heavy traffic; the API only signs, checks and records.
- **Signing subtlety:** inside Docker the API reaches the store as `s3:7070`, while browsers use `localhost:9000`. A signed URL includes the host name, so the API signs with the public address.
- **Local stand-in:** MinIO stopped publishing free Docker images in late 2025, so compose runs Versity S3 Gateway (31 MB, validates real S3 signatures). Moving to AWS is configuration only: bucket, region, and credentials or an IAM role.

## 8. AI extraction

- **Purpose:** paste a rate sheet or email and get a **draft** that pre-fills the form. It never saves anything; a person reviews and submits it.
- **Provider-independent:** built on `Microsoft.Extensions.AI` (`IChatClient`), configured for Gemini through Google's OpenAI-compatible endpoint. Switching to OpenAI or another compatible provider is configuration only.
- **Reliable output:** structured JSON that must match a schema; temperature 0; the prompt lists the allowed values and says to leave anything not stated empty rather than guess ("price on request" comes back with no price).
- **The draft is checked by the normal validator.** Problems come back as warnings with the same field keys, so the form highlights them.
- **Safety and cost controls:**
  - pasted text is treated as data, so instructions hidden in it are ignored (prompt-injection defence)
  - input capped at 20,000 characters
  - 30-second timeout
  - rate limit of 10 requests per minute per client
  - failures return a clean 502 without provider details
- **The app works fully without AI.** With no key, a disabled implementation is used, `/features` reports it as off, and the UI explains that.
- **Tests never call a real model.** A fake `IChatClient` returns fixed JSON, keeping CI free, fast and repeatable. `backend/scripts/ai_email_test.py` generates random supplier emails and scores the real model's answers.

## 9. Frontend

- **Stack:** React with TanStack Start, Router and Query; react-hook-form with zod; shadcn/ui and Tailwind; started in Lovable.
- **Types are generated from the OpenAPI document** (`npm run generate:api`). A compile-time check fails the build if the create form sends a field name the API doesn't know.
- **Mock mode:** `VITE_USE_MOCKS=true` (the default) runs on in-browser data that mimics the real API, so the frontend works without a backend.
- **Server errors map onto form fields**, e.g. `Services[0].Price` becomes `services.0.price`.
- **Branding:** Travelogic blue `#2599d6`, slate `#597480` and white. Buttons use a deeper blue (`#0076b2`) so white text passes the WCAG AA contrast minimum.

## 10. Security

- **CORS:** production allows only the listed origins, never "any origin"; local development allows any `localhost` port, because dev servers move to the next free one.
- **Containers run as non-root.**
- **Uploads:** file type checked from contents, storage keys generated by the server, short-lived signed URLs, and file traffic goes straight to S3.
- **Secrets** come from environment variables or git-ignored `.env` files. The development connection string contains a throwaway local password only.
- **Not done yet:** authentication and authorisation; the API is currently open.

## 11. Testing and delivery

- **115 automated backend tests:**
  - **72 unit tests:** domain rules, validators, handlers (with NSubstitute), file-type detection
  - **43 integration tests:** the real API against a real SQL Server and a real S3 gateway in containers (Testcontainers), covering signed-URL uploads, file-type rejection, duplicates, paging, CORS, rate limiting, AI (with a fake model) and the OpenAPI contract
- **Why real containers:** mocked databases hide real problems. These tests found an S3 bucket-region bug, a culture-formatting bug and a redirect-handling issue.
- **Docker Compose** starts SQL Server, S3 and the API with health checks and startup order, then migrates and seeds six suppliers.
- **CI (GitHub Actions)** builds and tests the backend, builds its Docker image, and type-checks and lints the frontend on every push.

## 12. Notable issues and how they were solved

| Problem | Resolution |
| --- | --- |
| Disk filled up and corrupted the SQL Server Docker image | Diagnosed "exec format error", removed and re-downloaded the image |
| Frontend and backend field names differed (`address`, `isActive`, AI warning shape) | Aligned the contract with a data-preserving migration; generated types stop it drifting again |
| A missing JSON setting would have broken every real AI extraction | Caught by integration tests before any real use |
| A price could be sent as a string, making the contract `number \| string` | Strict number handling, with a regression test |
| Locale-dependent formatting in the API docs | Invariant culture, plus a test that fails if the server's language leaks into the contract |
| MinIO images withdrawn | Switched to Versity S3 Gateway; production uses AWS S3 |
| AI provider overloaded (503) | Chose an available model; retries and a fallback model are future work |
| A `.gitignore` rule (`media/`) also hid the `Media` source folders (Windows is case-insensitive) | Removed the rule; one intermediate backend commit is incomplete, later ones are complete |

## 13. Known limitations and next steps

- Authentication and roles (who can create suppliers and upload files).
- Editing and deleting suppliers (the aggregate and concurrency token are ready).
- Rates by season and date range; availability.
- An S3 lifecycle rule to remove uploads that were never confirmed; thumbnails; virus scanning; a CDN in front of media.
- AI: retry with backoff and a fallback model when the provider is busy.
- Publish a `SupplierCreated` event through a transactional outbox, so other services can react.
- Run the frontend code inherited from Lovable through Prettier once, then enable the formatting rule in CI.
