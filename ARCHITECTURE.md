# WhaleWire Architecture Documentation

## Core Concepts

### Cursor
A value object that identifies an exact position in a blockchain.

- **Location**: `WhaleWire.Domain` (shared value object)
- **TON**: `(Lt, Hash)` — Lt = Logical Time + Transaction Hash
- **Future ETH**: `(BlockNumber, TxIndex)`
- **Format**: Always `"Primary:Secondary"` (both required, no empty strings)
- **Purpose**: Enables precise resumption after restarts, handles pagination

**Example:**
```csharp
var cursor = new Cursor(12345678, "abc123def456");
cursor.ToString(); // "12345678:abc123def456"
Cursor.Parse("12345678:abc123def456"); // Parses back
```

**Strict Validation:**
- Rejects format without secondary: `"12345"` → throws FormatException
- Rejects empty secondary: `"12345:"` → throws FormatException
- Both Primary and Secondary must be non-empty
- `TryParse()` for safe parsing

---

### BlockchainEvent
A normalized message representing a blockchain event, used for RabbitMQ messaging and persistence.

- **Location**: `WhaleWire.Messages`
- **Contains**: EventId, Chain, Provider, Address, Cursor, OccurredAt, RawJson
- **Purpose**: Single DTO for client output → message bus → persistence
- **Idempotency**: EventId is deterministic (same event → same ID)

**Fields:**
- `EventId`: Deterministic hash (16 hex chars) from SHA256(chain:address:lt:hash)
- `Chain`: "ton", "eth", "sol"
- `Provider`: "tonapi", "etherscan"
- `Address`: Tracked wallet/contract address
- `Cursor`: Position in blockchain (Primary + Secondary, e.g., Lt + Hash for TON)
- `OccurredAt`: UTC timestamp
- `RawJson`: Full raw response from provider

**Design:** Consolidated from previous `RawChainEvent` + `CanonicalEventReady` to eliminate redundancy. Uses `Cursor` value object for clean API.

---

### Checkpoint
Rich entity that persists the last cursor processed for a `(Chain, Address, Provider)` combination.

- **Location**: `WhaleWire.Infrastructure.Persistence.Entities`
- **Composite Key**: Chain + Address + Provider
- **State**: LastLt, LastHash (both required), UpdatedAt
- **Update Strategy**: Monotonic only (never moves backward)
- **Critical Rule**: Updated **only after successful persistence**, not during ingestion

**Domain Logic:**
- `Create()` factory method for initial checkpoint
- `Update(long lt, string hash)` with monotonic validation
- Throws `CheckpointConflictException` if same Lt with different hash (data corruption)

**Why monotonic updates matter:**
- Prevents re-processing on crashes
- Handles out-of-order event processing
- Detects provider data corruption early (Lt collision)

---

### Lease (AddressLease)
A distributed lock preventing multiple workers from processing the same address simultaneously.

- **Key format**: `"chain:provider:address"` (e.g., `"ton:tonapi:EQD..."`)
- **TTL**: 5 minutes (auto-expires if worker crashes)
- **Owner**: "ingestor" (worker identifier)
- **Purpose**: Prevents duplicate ingestion across horizontally scaled workers

---

### EventId
A deterministic identifier for blockchain events.

- **Generation**: `SHA256(chain:address:lt:hash).Substring(0, 16)`
- **Idempotency**: Same event → same ID → database unique constraint prevents duplicates
- **Properties**: Reproducible, collision-resistant, compact

---

## Layers & Components

### Domain Layer (`WhaleWire.Domain`)
**Responsibility**: Core domain models and value objects

**Contains:**
- **Value Objects**: `Cursor`
- **Services**: `EventIdGenerator`
- **Entities**: (Reserved for future rich domain models)

**Key Principle**: No infrastructure dependencies, pure domain logic

---

### Application Layer (`WhaleWire.Application`)
**Responsibility**: Blockchain-agnostic business logic

**Contains:**
- **Interfaces**: `IBlockchainClient`, `ICheckpointRepository`, `ILeaseRepository`, `IEventRepository`
- **Use Cases**: `IngestorUseCase`
- **DTOs**: `CheckpointData`
- **Messaging**: `IMessagePublisher`, `IMessageConsumer<T>`

**Key Principle**: Never references specific blockchain implementations (no `TonTransaction`, etc.)

---

### Infrastructure.Ingestion (`WhaleWire.Infrastructure.Ingestion`)
**Responsibility**: Blockchain-specific data fetching

**TON Implementation:**
- `TonApiClient : IBlockchainClient` — HTTP client for TonAPI
- `TonTransaction` — Provider-specific DTO (internal)
- Mapping: `TonTransaction` → `BlockchainEvent` (with EventId generation)

**Future implementations:**
- `EtherscanClient : IBlockchainClient`
- `SolanaClient : IBlockchainClient`

**Key Principle**: Adapts external APIs to generic `IBlockchainClient` interface

---

### Infrastructure.Persistence (`WhaleWire.Infrastructure.Persistence`)
**Responsibility**: Data storage with EF Core + PostgreSQL

**Entities:**
- `BlockchainEvent` — Stored events with unique constraint on EventId
- `Checkpoint` — Cursor tracking per (Chain, Address, Provider)
- `AddressLease` — Distributed locks with expiry

**Repositories:**
- `EventRepository` — Idempotent upsert (returns `wasInserted` bool)
- `CheckpointRepository` — Monotonic cursor updates
- `LeaseRepository` — Acquire/release with TTL

**Features:**
- Auto-migrations on startup
- Transactional consistency
- Indexed for query performance

---

### Infrastructure.Messaging (`WhaleWire.Infrastructure.Messaging`)
**Responsibility**: RabbitMQ pub/sub with resilience

**Components:**
- `RabbitMqPublisher` — Publishes to fanout exchange, persistent messages
- `RabbitMqConsumerService<T>` — Generic consumer with DLQ

**Resilience:**
- **Dead Letter Queue (DLQ)**: Failed messages after max retries
- **Exponential Backoff**: 1s → 5s → 30s
- **Max Retries**: 3 attempts
- **Circuit Breaker**: In handler (5 exceptions → 1 min break)

---

### Host Layer (`WhaleWire`)
**Responsibility**: Application entry point, worker services, handlers

**Workers:**
- `IngestionWorkerService` — Periodic polling (every 30s) to fetch new events
- `SchedulerService` — Test publisher (temporary, replaced by real ingestion)

**Handlers:**
- `BlockchainEventHandler` — Consumes messages, persists events, updates checkpoints

**Configuration:**
- Multi-layer: appsettings.json → environment variables
- Typed options: `TonApiOptions`, `CircuitBreakerOptions`, `SchedulerOptions`

---

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                      IngestionWorkerService                      │
│  (Background loop every 30s, per configured address)             │
└────────────┬─────────────────────────────────────────────────────┘
             │
             ▼
    ┌────────────────────┐
    │ IngestorUseCase    │
    │ ExecuteAsync()     │
    └────────┬───────────┘
             │
    ┌────────▼─────────────────────────────────────────────────┐
    │ 1. TryAcquireLease("ton:tonapi:EQD...")                  │
    │    └─> LeaseRepository → PostgreSQL AddressLease         │
    │    If not acquired → exit (another worker has it)        │
    └──────────────────────────────────┬───────────────────────┘
                                       │
    ┌──────────────────────────────────▼───────────────────────┐
    │ 2. GetCheckpoint("ton", "EQD...", "tonapi")              │
    │    └─> CheckpointRepository → PostgreSQL Checkpoint      │
    │    Returns Cursor (12345678, "abc123") or null           │
    └──────────────────────────────────┬───────────────────────┘
                                       │
    ┌──────────────────────────────────▼───────────────────────┐
    │ 3. GetEventsAsync(address, cursor, limit=100)            │
    │    └─> TonApiClient (IBlockchainClient)                  │
    │        ├─> HTTP GET to TonAPI                            │
    │        ├─> Deserialize TonTransaction[]                  │
    │        ├─> Generate EventId (deterministic)              │
    │        └─> Map to BlockchainEvent[]                      │
    └──────────────────────────────────┬───────────────────────┘
                                       │
    ┌──────────────────────────────────▼───────────────────────┐
    │ 4. For each BlockchainEvent:                             │
    │    └─> PublishAsync to RabbitMQ (already has EventId)    │
    └──────────────────────────────────┬───────────────────────┘
                                       │
    ┌──────────────────────────────────▼───────────────────────┐
    │ 5. ReleaseLease("ton:tonapi:EQD...")                     │
    │    (Always executes, even if errors via finally block)   │
    └──────────────────────────────────────────────────────────┘

                           │
                           │ RabbitMQ Queue
                           ▼

    ┌──────────────────────────────────────────────────────────┐
    │              BlockchainEventHandler                      │
    │  (RabbitMqConsumerService triggers on new messages)      │
    └────────┬─────────────────────────────────────────────────┘
             │
    ┌────────▼─────────────────────────────────────────────────┐
    │ Circuit Breaker wrapper (5 exceptions → 1 min break)     │
    └────────┬─────────────────────────────────────────────────┘
             │
    ┌────────▼─────────────────────────────────────────────────┐
    │ 1. UpsertEventIdempotent(eventId, chain, address, ...)   │
    │    └─> EventRepository → PostgreSQL BlockchainEvent      │
    │    Returns bool: wasInserted                             │
    │    (Unique constraint on EventId prevents duplicates)    │
    └────────┬─────────────────────────────────────────────────┘
             │
    ┌────────▼─────────────────────────────────────────────────┐
    │ 2. IF wasInserted:                                       │
    │    UpdateCheckpointMonotonic(chain, address, provider,   │
    │                              cursor.Primary, cursor.Secondary) │
    │    └─> CheckpointRepository → PostgreSQL Checkpoint      │
    │    Uses Checkpoint.Update() with conflict detection      │
    └──────────────────────────────────────────────────────────┘

             │
             ▼
    ┌──────────────────┐
    │  ACK to RabbitMQ │  ← Message removed from queue
    └──────────────────┘

    If exception thrown:
    ├─> Retry with exponential backoff (1s, 5s, 30s)
    ├─> After 3 retries → Dead Letter Queue
    └─> Circuit breaker tracks failures
```

---

## Testing Strategy

### Unit Tests (`WhaleWire.Tests.Unit`)
- **Target**: Repositories, EventIdGenerator
- **Speed**: < 1s total
- **DB**: In-memory SQLite via `InMemoryDbContextFixture`
- **Existing**: 15 tests (EventRepository, CheckpointRepository, LeaseRepository)

### Integration Tests (`WhaleWire.Tests.Slow`)
- **Target**: End-to-end flows with real infrastructure
- **Infrastructure**: Testcontainers (Postgres + RabbitMQ)
- **Existing**: Idempotency, Checkpoint flows
- **Future**: IngestorUseCase integration test

### E2E Tests (Future)
- **Target**: Full system with real TonAPI (or mocked provider)
- **Validation**: No gaps, no dupes over 30+ min run
- **Restart safety**: Kill worker mid-run → restart → verify continuity

---

## Configuration Example

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=whalewire;Username=whalewire;Password=***",
    "RabbitMQ": "amqp://guest:guest@localhost:5672/"
  },
  "TonApi": {
    "BaseUrl": "https://tonapi.io",
    "ApiKey": "",
    "DefaultLimit": 100
  },
  "CircuitBreaker": {
    "ExceptionsAllowedBeforeBreaking": 5,
    "DurationOfBreakMinutes": 1
  },
  "Ingestion": {
    "PollingIntervalSeconds": 30,
    "Addresses": [
      "EQD...",
      "EQA..."
    ]
  }
}
```

---

## Future Enhancements (Chapter 9+)

1. **Outbox Pattern**: Transactional message publishing
2. **Alert Intents**: Event → Alert rules → Notification queue
3. **Multi-Chain**: ETH, Solana clients via same `IBlockchainClient`
4. **Horizontal Scaling**: Multiple workers with lease coordination
5. **Metrics**: Prometheus metrics for ingestion rate, lag, errors
6. **Admin API**: Trigger manual ingestion, view checkpoints, pause/resume

---

## Glossary

- **Lt (Logical Time)**: TON's global transaction counter (monotonically increasing)
- **Idempotency**: Processing same input multiple times produces same result
- **Monotonic Update**: Value only increases, never decreases
- **Circuit Breaker**: Stops calling failing service temporarily to prevent cascade failures
- **DLQ (Dead Letter Queue)**: Queue for messages that failed max retries
- **Lease**: Temporary exclusive lock with auto-expiry
- **Cursor**: Bookmark in blockchain data stream
- **Fanout Exchange**: RabbitMQ routing that sends messages to all bound queues
