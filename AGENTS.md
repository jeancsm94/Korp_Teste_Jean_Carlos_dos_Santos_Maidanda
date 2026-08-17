# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Sistema de emissão de Notas Fiscais — technical test. The backend is a **two-microservice** ASP.NET Core solution (Estoque + Faturamento), each with its own physical SQLite database, communicating over HTTP with Polly-based resilience. The Angular frontend (`Frontend/`) is implemented as a standalone Angular 18 app (Angular Material + RxJS), with no code dependency on the backend — it consumes both APIs over HTTP.

## Commands

Run all commands from the repository root (where the `.slnx` solution file lives).

- Restore & build: `dotnet build`
- Run Estoque.Api: `dotnet run --project Services/Estoque.Api` → `http://localhost:5100` (HTTPS profile also serves `https://localhost:7100`)
- Run Faturamento.Api: `dotnet run --project Services/Faturamento.Api` → `http://localhost:5200` (HTTPS profile also serves `https://localhost:7200`)
- Both services must be running (in separate terminals) for Faturamento's invoice creation/printing to work, since it calls out to Estoque.
- EF Core migrations use a local `dotnet-ef` tool (manifest at `.config/dotnet-tools.json`, restored automatically by `dotnet tool restore` or the first `dotnet build`/`dotnet restore`). To add/apply migrations:
  ```
  dotnet ef migrations add <Name> --project Services/Estoque.Api --startup-project Services/Estoque.Api -o Data/Migrations
  dotnet ef database update --project Services/Estoque.Api --startup-project Services/Estoque.Api
  ```
  (same pattern for `Services/Faturamento.Api`). Each service's SQLite file (`estoque.db`, `faturamento.db`) lives alongside its `.csproj` and is gitignored.
- There are no test projects yet.

## Architecture

```
Korp_Teste_Jean_Carlos_dos_Santos_Maidanda.slnx
├── Shared/Korp.Shared/              class library — domain exception hierarchy + shared IExceptionHandler
├── Services/Estoque.Api/            product/stock microservice — port 5100
└── Services/Faturamento.Api/        invoice microservice — port 5200
```

Target framework `net10.0` everywhere, nullable reference types + implicit usings enabled, controller-based MVC (`[ApiController]` + `[Route("[controller]")]`), braced namespaces, plain classes with `{ get; set; }` (not records) for entities/DTOs — records are used only for small immutable value shapes (HTTP client DTOs, debit result items).

### Estoque.Api (porta 5100)

- `Models/Product.cs`, `Models/ProcessedRequest.cs` (idempotency log).
- `Data/EstoqueDbContext.cs` — SQLite (`estoque.db`), unique index on `Product.Code` and on `ProcessedRequest.IdempotencyKey`.
- `Services/ProductService.cs` — CRUD + `DebitBatchAsync`, the core concurrency/idempotency logic:
  - Idempotency: looks up `ProcessedRequests` by the caller-supplied `Idempotency-Key` header first; if found, replays the cached JSON response instead of re-debiting.
  - Concurrency: debits via a **guarded atomic UPDATE** (`ExecuteUpdateAsync` with `WHERE Balance >= quantity`, checking affected-row count) rather than read-then-write — this is what actually prevents a negative balance under concurrent requests, not SQLite's single-writer behavior. All items in a batch are all-or-nothing (transaction rolled back if any item has insufficient stock).
- `Controllers/ProductsController.cs` — `POST /products`, `GET /products[?search=]`, `GET /products/batch?ids=1,2,3`, `GET/PUT/DELETE /products/{id}`, `POST /products/debit-batch` (requires `Idempotency-Key` header).

### Faturamento.Api (porta 5200)

- `Models/Invoice.cs`, `InvoiceItem.cs` (snapshots `ProductCode`/`ProductDescription` at creation time — no physical FK to Estoque's DB, separate databases), `InvoiceCounter.cs` (single-row counter for sequential `Number` generation, incremented via the same guarded-`ExecuteUpdateAsync` pattern to avoid a `MAX()+1` race).
- `Data/FaturamentoDbContext.cs` — SQLite (`faturamento.db`), `Status` stored as string.
- `Clients/IEstoqueClient.cs` / `EstoqueClient.cs` — typed `HttpClient` calling Estoque's `/products/batch` (invoice creation validation) and `/products/debit-batch` (printing). Registered in `Program.cs` via `AddHttpClient<IEstoqueClient, EstoqueClient>().AddStandardResilienceHandler(...)` (`Microsoft.Extensions.Http.Resilience`, Polly v8): 3 retries with exponential backoff+jitter, 3s per-attempt timeout, circuit breaker (opens after 2+ failures within a 10s window, breaks for 10s), 20s total timeout.
- `Services/InvoiceService.cs` — `CreateAsync` (validates products via Estoque, assigns sequential `Number`), `PrintAsync` (rejects if not `Aberta`; calls `IEstoqueClient.DebitBatchAsync` with idempotency key `invoice-{id}` **before** flipping status to `Fechada` — if the Estoque call throws for any reason, the status-flip line is never reached, so the invoice cleanly stays `Aberta` with no manual rollback needed).
- `Controllers/InvoicesController.cs` — `POST /invoices`, `GET /invoices[?status=]`, `GET /invoices/{id}`, `POST /invoices/{id}/print`.
- `Web/EstoqueUnavailableExceptionHandler.cs` — Faturamento-only `IExceptionHandler`, registered before the shared one, maps `BrokenCircuitException`/`TimeoutRejectedException`/`HttpRequestException` (from the Polly pipeline) to `503` with a user-facing Portuguese message.

### Shared error handling (`Shared/Korp.Shared`)

`Exceptions/DomainException.cs` (abstract, carries `StatusCode`/`Title`) with `NotFoundException` (404), `ConflictException`/`InvalidInvoiceStateException`/`InsufficientStockException` (409, the last carries a list of `{ productId, requested, available }` as a ProblemDetails extension). `Web/ApiExceptionHandler.cs` implements `IExceptionHandler`, registered via `AddExceptionHandler<T>()` + `AddProblemDetails()` + `app.UseExceptionHandler()` in both services, producing consistent RFC 7807 `ProblemDetails` responses. LINQ is used throughout the service layer (`Where`, `Select`, `OrderBy`, `AnyAsync`, `ToDictionaryAsync`-equivalents, `ExecuteUpdateAsync`) — see `ProductService.DebitBatchAsync` and `InvoiceService.CreateAsync`/`ListAsync` for representative examples.

### Frontend (`Frontend/`, porta 4200)

Standalone Angular 18 app (Angular Material + RxJS), independent npm project with no build/code dependency on the .NET solution — it only talks to Estoque/Faturamento over HTTP, via URLs configured in `Frontend/src/environments/environment.ts`.

- `core/` — HTTP error interceptor, shared `ProblemDetails` model, `NotificationService`.
- `features/produtos/` and `features/notas-fiscais/` — one feature module each, with a `*-store.service.ts` (BehaviorSubject-backed cache + list/CRUD state), an HTTP `*.service.ts`, and list/form/detail standalone components.
- `shared/ui/` — reusable `confirm-dialog` and `status-badge` components.
- Run with `cd Frontend && npm install && ng serve`; requires both backend services running.

### Manual test scripts

See the "Anexo A" test scripts (failure/recovery and concurrency scenarios) originally captured in the implementation plan — both were run manually against this implementation and confirmed working:
- Stopping Estoque mid-flight → `print` retries, then returns 503 with the invoice staying `Aberta`; restarting Estoque and retrying succeeds with the stock debited exactly once (idempotency confirmed).
- Two near-simultaneous `print` calls against a product with balance 1 → one succeeds (`Fechada`), the other gets a clean 409 with `insufficient stock` detail; balance never goes negative.

No database, authentication config beyond the above exists. AI-assisted product-description suggestion was considered but deliberately deferred (not implemented) per explicit user decision — add it later as a new endpoint in Estoque.Api if requested.