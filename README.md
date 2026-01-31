# WhaleWire

Automatic TON blockchain whale tracker with real-time alerts.

---

## Quick Start

### 1. Configure Secrets

```bash
# Copy template
cp env.example .env

# Edit .env and add your Chainstack API key
# Get free key at: https://chainstack.com
```

**`.env` file:**
```env
CHAINSTACK_API_KEY=your_actual_key_here
POSTGRES_PASSWORD=whalewire_dev
RABBITMQ_PASSWORD=whalewire_dev
```

---

### 2. Run with Docker

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f whalewire

# Check database
docker exec -it whalewire-postgres psql -U whalewire -d whalewire
```

**SQL to verify:**
```sql
-- Top discovered whales
SELECT address, balance FROM monitored_addresses ORDER BY balance DESC LIMIT 10;

-- Events processed
SELECT COUNT(*) FROM events;

-- Latest checkpoint
SELECT * FROM checkpoints;
```

---

### 3. Stop

```bash
docker compose down
```

---

## How It Works

```
1. DiscoveryWorker (every 6 hours)
   → Chainstack API: top 1000 TON accounts
   → Saves to monitored_addresses table

2. IngestionWorker (every 30 seconds)
   → For each monitored address:
   → TonAPI: fetch new transactions
   → Publish to RabbitMQ
   → Persist to events table
   → Update checkpoint

3. Auto-scales: 1000 addresses @ 1 rps = manageable
```

---

## Architecture

- **Domain**: Value objects (Cursor), Services (EventIdGenerator)
- **Application**: Use cases (blockchain-agnostic)
- **Infrastructure**: TON client, Chainstack client, Postgres, RabbitMQ
- **Tests**: 49 unit tests, 2 integration tests

See [ARCHITECTURE.md](ARCHITECTURE.md) for details.

---

## Development

**Run tests:**
```bash
dotnet test
```

**Migrations:**
```bash
# Auto-applied on startup
# Manual: dotnet ef migrations add Name -p WhaleWire.Infrastructure.Persistence -s WhaleWire
```

---

## Configuration

Key settings in `appsettings.json` or `.env`:

```json
{
  "Discovery": {
    "Enabled": true,
    "PollingIntervalMinutes": 360,
    "TopAccountsLimit": 1000
  },
  "Ingestion": {
    "Enabled": true,
    "PollingIntervalSeconds": 30
  }
}
```

**Note:** Discovery polls every 6 hours to stay within Chainstack free tier (3000 req/month).

---

## Monitoring

**Health:** http://localhost:5007/health (TODO)

**Logs:**
```bash
docker compose logs -f whalewire | grep "Discovery\|Ingestion"
```

---

## Troubleshooting

**No addresses discovered:**
- Check Chainstack API key in `.env`
- Check logs: `docker compose logs whalewire`

**No events:**
- Wait 6 hours for first discovery cycle OR manually insert addresses
- TonAPI rate limit (1 rps) - adjust polling if needed

**Database issues:**
- Migrations auto-apply on startup
- Check: `docker compose logs postgres`

---

## Next Steps (Chapter 9)

- [ ] Alert rules (whale transfers > X TON)
- [ ] Telegram/Discord notifications
- [ ] Admin API (pause/resume, manual address add)
- [ ] Metrics & observability

---

## License

MIT
