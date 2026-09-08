# Rebtel Library System

Library API split into two processes: a REST Api in front, a gRPC Service behind it, Postgres as the store. `docker-compose up` starts everything.

The warm-up exercises from the brief are in `warmup/LibrarySystem.Warmups`. They are a separate project with their own tests, not mixed into the API.

## Requirements

- **Docker Desktop** (or Docker Engine + Compose). Needed to run the system and for integration/system tests.
- **.NET 10 SDK.** Needed for `dotnet test` on the host. `docker-compose up --build` does not require it — the images build inside the `sdk:10.0` container.

## Run

```
docker-compose up --build
```

Starts Postgres, Seq, the Service, and the Api. First run migrates the database and seeds 1,000 books, 2,000 readers, and ~25,000 loans. The seed is fixed (`200646`), so the IDs below are the same on every clean start.

- Api: http://localhost:5000 — Swagger at `/swagger`
- Health: http://localhost:5000/health
- Seq: http://localhost:8081 — no login

- Sample reader: `00000000-0000-0000-0000-000000000002`
- Sample book (available): `00000000-0000-0000-0000-000000000001`
- Sample loaned book: `00000000-0000-0000-0000-000000000003`
- Sample active loan: `00000000-0000-0000-0000-000000000004`

Open http://localhost:5000/swagger. Try it out is pre-filled — Execute as-is. Borrow has two examples (available vs already on loan). Return uses the seeded active loan.

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/inventory/most-borrowed?daysLookback=30&maxResults=10` | Most borrowed books in a time window |
| GET | `/api/readers/most-active?startDate=&endDate=&maxResults=10` | Most active readers in a time window |
| GET | `/api/readers/{readerId}/reading-pace` | Estimated pages/day from borrow/return duration |
| GET | `/api/books/{bookId}/borrowing-patterns?maxResults=3` | Other books borrowed by people who borrowed this one |
| POST | `/api/loans` | Borrow a book (`{ "bookId": "...", "readerId": "..." }`) |
| POST | `/api/loans/{loanId}/return` | Return a book |

`maxResults` is optional (defaults in the table) and must be between 1 and 100.

Suggested path in Swagger (Execute, no typing):

1. The four GETs.
2. POST `/api/loans` with example **available** → `201`. Same again → `409`.
3. POST `/api/loans` with example **alreadyOnLoan** → `409`.
4. POST `/api/loans/{loanId}/return` (seeded active loan) → `200`. Same again → `409`.

## Structure

```
warmup/   LibrarySystem.Warmups
src/      LibrarySystem.Api, LibrarySystem.Service, LibrarySystem.Contracts
tests/    Unit, Functional, Integration, System
```

**Api** — HTTP only. Validates the request, calls the matching gRPC method, maps the gRPC status to an HTTP status. No domain logic.

**Service** — `Book`, `Reader`, `Loan`, EF Core, and the queries behind each RPC.

**Contracts** — `.proto` files plus small shared types (`Result<T>`, paging/length limits). Both sides compile against this instead of duplicating DTOs.

Folders follow the features (`InventoryInsights`, `UserActivity`, `BorrowingPatterns`, `Loans`) on both the Api and the Service, so the HTTP endpoint and the RPC that handles it sit under the same name. Shared code (`Domain`, `DbContext`, interceptors) lives outside the slices.

gRPC is what the assignment asked for between the two layers. It also stops HTTP details (status codes, query strings, Swagger) from leaking into the Service. The Api does not add caching or retries.

## Data and concurrency

Postgres + EF Core. The model is relational (books, readers, loans, joins for co-borrowing), so SQL was the straightforward option.

Two concurrent cases are enforced in the database, not with a check-then-insert in code:

- **Borrow** — partial unique index `loans(BookId) WHERE ReturnedAtUtc IS NULL`. A book can have many historical loans, but only one active. Two borrows of the same book at the same time: one `201`, one `409`.
- **Return** — optimistic concurrency via Postgres `xmin`, mapped as an EF shadow `uint` with `IsRowVersion()`. No extra version column. Two returns of the same loan: one `200`, one `409`.

The Service catches the unique violation and the concurrency exception and maps them to gRPC `FailedPrecondition` / `Aborted`, which the Api turns into `409`. That covers two requests racing on the same book or loan. It does not cover a client sending the same POST twice on purpose (idempotency keys); those are different problems.

## Validation and errors

FluentValidation on both layers, limited to request shape: valid GUIDs, parseable dates, `startDate <= endDate`, `maxResults` in range. Whether a book exists, or a loan is already returned, is handled in the Service next to the query.

Api: `ValidationFilter<T>` endpoint filter → `400` with field errors.
Service: `ValidationInterceptor` on the gRPC pipeline → `InvalidArgument`.

gRPC errors are mapped to HTTP in `GrpcExceptionHandler`. Endpoints don't wrap calls in try/catch. Anything else hits `GlobalExceptionHandler` and becomes a `500` without internals in the body. Malformed JSON is treated as `400` (`BadHttpRequestException`), not `500`.

## Logging

Serilog to the console and Seq, using message templates so properties (`BookId`, `MaxResults`, etc.) are fields, not pasted into a string. ASP.NET Core creates a trace per request and the gRPC client forwards it, so Api and Service log lines for the same call share a `TraceId`. Filter Seq by that id to follow one request.

Health checks on `/health` for both processes. The Api probes the Service over HTTP; the gRPC port is HTTP/2 and won't answer a normal GET.

## Tests

```
dotnet test Rebtel.Library.slnx
```

Runs the warm-up tests and the four projects below. The solution file is required because the folder also has `docker-compose.dcproj`. Unit and functional tests only need the .NET SDK. Integration and system tests use [Testcontainers](https://dotnet.testcontainers.org/) for a disposable Postgres, so Docker has to be running. No connection strings to set; the container is disposed at the end of the run.

| Project | Tests | Scope |
|---|---|---|
| `LibrarySystem.Warmups` | 22 | The four starter methods |
| `LibrarySystem.UnitTests` | 73 | Domain rules and FluentValidation, no I/O |
| `LibrarySystem.FunctionalTests` | 49 | Service methods on EF InMemory; Api HTTP pipeline with fake gRPC clients |
| `LibrarySystem.IntegrationTests` | 8 | Real Postgres: concurrent borrow/return, migrations, unique email. Reset with [Respawn](https://github.com/jbogard/Respawn) |
| `LibrarySystem.SystemTests` | 7 | HTTP → gRPC → Service → Postgres, both hosts in-process |

The unique index and `xmin` checks only show up against real Postgres, which is why they are integration tests and not InMemory.

## Notes

- On first start the Service may log `libgssapi_krb5.so.2` and a failed `__EFMigrationsHistory` query. That is Npgsql against an empty database. Migration creates the table right after.
- The first `dotnet test` that hits Testcontainers will pause while Docker pulls/starts Postgres. Not a hang.
- To wipe the database and seed again: `docker compose down -v`, then `docker compose up --build`.

## Not done

- **Auth.** Endpoints are open because the brief said so. On a real service I’d authenticate at the Api (OIDC/JWT) and keep the Service on a private network. I wouldn’t copy auth into both processes.
- **Max loans per reader.** There is no cap. The brief never defined one, so I didn’t invent it. If a librarian names a number, it’s another partial unique index or a counted check next to the existing “one active loan per book” rule.
- **Idempotency keys.** Concurrent borrow/return is handled. A client retrying the same `POST /api/loans` is a different problem — you’d store a key and return the original loan instead of inserting again.
- **Book/reader write APIs.** No create/update/delete over HTTP. Rows come from the seed. Adding them is straightforward; they weren’t asked for.
- **Recommendations at scale.** The co-borrow query is a two-hop join (book → readers → other books). Fine at ~25k loans. If it got heavy I’d precompute affinity counts on a schedule, still in Postgres. A graph database (Neo4j or similar) only starts to pay off if the walks get deeper, weighted, or many relationship types — not for this single top-N query.
- **Caching / retries.** The Api does not cache reports or retry gRPC. Nothing in the brief needed it, and retries without idempotency make the write path worse.
- **Two Service replicas.** The unique index and `xmin` live in Postgres, so a second replica doesn’t break borrow/return. In-memory state isn’t the source of truth.
- **OpenTelemetry.** `TraceId` in Seq already ties an Api request to its Service call. A full OTel pipeline would be the next step if this had to plug into an existing company stack — not a second log product for this repo.
- **Pessimistic locking.** I used optimistic concurrency (`xmin`) on return, not `SELECT FOR UPDATE`. Last-write-wins with a 409 is enough. Pessimistic locking would serialize returns for no gain here.
- **Secrets / TLS in compose.** Password in `docker-compose.override.yml`, HTTP on port 5000, Seq with no login. Local review only. In a real deploy the connection string comes from a secret store and the Api sits behind TLS.
