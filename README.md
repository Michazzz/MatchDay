# MatchDay

Event-driven microservices demo built with .NET, RabbitMQ and MassTransit — showcasing async
messaging, an orchestrated saga, Docker Compose deployment, and pub/sub patterns.

The domain is a (fake) match ticket reservation and sales platform: browse matches, reserve a seat,
pay, and get a ticket — implemented as independent services that communicate over a message broker.

## Architecture

| Service | Responsibility | Phase |
|---|---|---|
| **MatchService** | Match catalog (teams, date, stadium, sector availability). CRUD, read-heavy. | 1 |
| **ReservationService** | Seat reservation + saga orchestration: `Reserved → PaymentPending → Confirmed / Cancelled`. | 1 → 2 |
| **PaymentService** | Simulated payment (random success/failure) to demonstrate saga compensation. | 2 |
| **NotificationService** | Sends notifications on reservation/payment events. | 1 |
| **TicketService** | Generates the ticket (PDF/QR) after a successful payment. | 3 (optional) |
| **MatchDayContracts** | Shared event/message contracts. | — |
| **MatchDayGateway** | API Gateway (YARP) — single entry point for the frontend. | — |

Each service owns its database (database-per-service). Services talk asynchronously via RabbitMQ.

## What it demonstrates

- **A real saga** — reservation → payment → confirmation/compensation is a genuine multi-step process,
  not a contrived example.
- **Idempotency & race conditions** — two users reserving the same seat at the same time.
- **Independent scaling** — read-heavy `MatchService` vs write-spiky `ReservationService`.
- **Resilience & messaging patterns** — competing consumers, retries/back-off (Polly), API Gateway.

## Roadmap

- **Phase 1 (MVP):** MatchService + ReservationService + NotificationService.
- **Phase 2 (saga):** + PaymentService + the `Reserved → PaymentPending → Confirmed/Cancelled` state machine.
- **Phase 3 (optional):** TicketService.

## Tech stack

.NET 10 · ASP.NET Core Minimal APIs · EF Core + PostgreSQL · RabbitMQ + MassTransit · YARP · Docker Compose

## Running locally

Requires the .NET 10 SDK and Docker.

```bash
# Build & run everything currently wired into compose
docker compose up --build

# MatchService: http://localhost:5172/health
```

Or run a single service directly:

```bash
dotnet run --project src/MatchService
```

## Repository layout

```
MatchDay/
├─ src/                       # services live here
│  └─ MatchService/
├─ tests/                     # test projects
├─ Directory.Build.props      # shared build settings
├─ Directory.Packages.props   # central package versions
├─ docker-compose.yml
└─ global.json                # pinned SDK
```

## License

[MIT](LICENSE)
