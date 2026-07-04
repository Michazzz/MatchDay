# Learning Log

MatchDay is a portfolio project I build to genuinely learn microservices — not to
have them generated for me. I work in a mentor-driven loop: I design and write the
C# myself, then get a code review, fix what it catches, and record the reasoning here.

This log is the "why" behind the commits. Each entry captures the design decisions I
made, the trade-offs I weighed, and the mistakes that review caught — so the thinking
is visible, not just the final code. The small, incremental Git history is the
companion to this file.

> Foundation, **MatchService** (catalog + REST) and **Phase 1** (event-driven
> reservation flow across MatchService / ReservationService / NotificationService over
> RabbitMQ + MassTransit) were built and end-to-end verified before this log began —
> including a race-condition proof (12 concurrent reservations on a 3-seat sector →
> exactly 3 reserved, 9 rejected, no oversell). This log starts at Phase 2.

---

## Phase 2 — Payment & Saga

Goal of the phase: add a payment step and coordinate the whole process with a **saga**
(state machine) that orchestrates the flow and **compensates** on failure. Core lesson:
in a distributed system there is no single ACID transaction across services —
consistency is built explicitly, step by step, with compensation.

### PS-1 — Payment message contracts

**What I built:** `ProcessPayment` (command), `PaymentCompleted` and `PaymentFailed`
(events) in `src/MatchDayContracts/PaymentMessages.cs`.

**Decisions & lessons:**
- **`decimal` for money, never `int` or `double`.** My first draft used `int Price`.
  `int` silently drops the grosze (149.99 → 149); `double`/`float` are binary
  floating-point and accumulate rounding errors — never use them for money. `decimal`
  is exact and maps 1:1 to Postgres `numeric`. This was the main takeaway of the task.
- **Command vs event naming.** Command = imperative, single consumer (`ProcessPayment`);
  event = past tense, broadcast (`PaymentCompleted` / `PaymentFailed`). "Failed" (it
  didn't go through) reads better than "Rejected" (someone rejected it) for a payment.
- **Contracts are designed with intent before the consumer exists.** The PaymentService
  consumer isn't written yet, but the contract already declares who produces and who
  consumes each message.
- **`ReservationId` as the correlation id** across the whole process — reused
  everywhere (SeatHold PK, idempotency, message key) rather than inventing new keys.

**Review caught:** `int` → `decimal`; and doc-comments with the producer/consumer
backwards (a copy-paste leftover that said MatchService/ReservationService instead of
PaymentService) — fixed to state the real producer of each message.

### PS-2 — Enrich `SeatsReserved` with the total price

**What I built:** added `TotalPrice` to `SeatsReserved`; MatchService now computes
`Sector.Price * Quantity` in `ReserveSeatsConsumer` and both persists it on `SeatHold`
and publishes it on the event. New EF migration `AddSeatHoldTotalPrice`.

**Decisions & lessons:**
- **Event enrichment over synchronous querying.** ReservationService needs the amount
  to charge, but the price lives in MatchService (`Sector.Price`) — it is *not* in the
  reservation's own database. Instead of ReservationService calling MatchService over
  HTTP (tight coupling), the owner of the data (MatchService) injects the amount into
  the `SeatsReserved` event it already emits. Data flows downstream from its owner, at
  the moment the owner already has it — no extra coupling.
- **Data ownership.** This made the database-per-service boundary concrete: the
  reservation record simply doesn't know the price, and that's correct.
- **Idempotency of the enriched event.** The redelivery path re-publishes `SeatsReserved`
  but doesn't hold the sector row. I chose to persist `TotalPrice` on `SeatHold` (rather
  than re-reading the sector), so a redelivery reproduces the exact amount originally
  charged — even if the sector price changed in the meantime. The compiler surfaced this
  the moment I threaded the price through: the idempotent branch had no price to pass.
- **Migration backfill.** Adding a `NOT NULL decimal` column to a table that may already
  hold rows forces a `defaultValue: 0m` — EF backfills existing rows. Fine for a demo,
  but a real system with historical data would need a deliberate default.
- **Decimal precision.** Configured `HasPrecision(10, 2)` on `SeatHold.TotalPrice` to
  match `Sector.Price` → column type `numeric(10,2)`, consistent across the schema.

**Review caught:** minor style only (an explicit `decimal` local where the file uses
`var`); the design was sound.

### PS-3 — PaymentService skeleton

**What I built:** a new `PaymentService` (Web SDK) that connects to RabbitMQ and owns its
own Postgres — no payment logic yet (that's PS-4). `Payment` entity + `PaymentStatus` enum,
`PaymentDbContext` + design-time factory, `Program.cs` wiring (MassTransit + EF +
`MigrateAsync` + `/health`), Dockerfile (with krb5), a `paymentservice` + `paymentservice-db`
pair in docker-compose, and an `InitialCreate` migration. Verified live: `/health` responds,
the `Payments` table is created, and the MassTransit bus starts against RabbitMQ.

**Decisions & lessons:**
- **`Payment` is keyed by `ReservationId`** (not a surrogate `Id`) — same idempotency
  pattern as `SeatHold`: a redelivered `ProcessPayment` would collide on the primary key
  instead of creating a duplicate payment.
- **EF key convention gotcha.** EF only auto-detects a primary key named `Id` or
  `<Type>Id` (`PaymentId`). `ReservationId` doesn't match, so the key must be declared
  explicitly (`HasKey`) — otherwise migration generation fails with "requires a primary
  key". Caught in review before it bit.
- **A "worker" service is not an API service.** PaymentService (like NotificationService)
  consumes messages and exposes only `/health` — so no OpenAPI/Scalar and no
  `JsonStringEnumConverter`. Copying the wrong template (an API service) dragged in
  packages and wiring that had to be stripped back out.
- **Database-per-service, again.** Its own Postgres (`paymentdb`, port 5435), its own
  `DbContext` and migration — nothing shared with the other services' databases.

**Review caught:** several copy-paste leftovers from the ReservationService template —
`DbSet<Payment> Reservations` (would have created a `Reservations` table), a `/health`
reporting `"ReservationService"`, a compose `ConnectionStrings__ReservationDb` key and a
`depends_on: reservationservice-db` (both would have crashed startup), and the migration
landing in the default `Migrations/` folder instead of `Data/Migrations/`. Lesson: after
pasting a template, read every line and substitute the names — the compiler won't catch a
wrong-but-valid string.
