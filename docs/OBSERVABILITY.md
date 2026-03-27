# WhaleWire Observability Plan

**Strategy:** [ADR 0001 — Observability principles](adr/0001-observability-principles.md). **Follow-ups (lag scope, whale logs, notifiers):** [ADR 0002](adr/0002-monitoring-and-notifications-follow-ups.md).

## Implementation Status

| Item | Status |
|------|--------|
| Prometheus metrics (`/metrics`) | Done |
| `whalewire_events_ingested_total` | Done |
| `whalewire_event_lag_seconds` | Done |
| `whalewire_alerts_fired_total` | Done |
| `whalewire_circuit_breaker_state` | Done |
| `whalewire_dlq_messages_total` | Done |
| `whalewire_discovery_addresses_total` | Done |
| `whalewire_discovery_last_success_timestamp_seconds` | Done |
| Alerting rules | Done |
| Correlation IDs | Done |
| Grafana (compose, overview dashboard) | Done |

**Evidence:** Discovery failure test, HTTP-mocked TonCenter test, `/metrics` E2E test (`MetricsEndpointE2ETests`), `whalewire_discovery_last_success_timestamp_seconds` E2E assertion, Prometheus rules validation (`PrometheusRulesValidationTests` via promtool).

---

## Why Prometheus?

- **Pull model** — No push infrastructure, no firewall changes. Prometheus scrapes `/metrics`. Works in restricted environments.
- **Time-series native** — Counters for rates, gauges for lag/state. Built for operational metrics.
- **Alertmanager** — Rules + routing. One DLQ message → alert. No external alerting service required.
- **K8s/Docker standard** — ServiceMonitor, annotations, Grafana dashboards. Fits container deployments.
- **Low overhead** — Expose metrics; scrape every 15–30s. No in-process aggregation or buffering.
- **Vendor-neutral** — Open format. Can add OpenTelemetry export later without replacing Prometheus.

**Alternatives:** OpenTelemetry (metrics + traces, more setup). App Metrics (less ecosystem). Prometheus is the pragmatic choice for "ingestion rate, lag, alerts, circuit breaker."

---

## What We Need to Do

### 1. Prometheus Metrics (required)

| Metric | Type | Labels | Purpose |
|--------|------|--------|---------|
| `whalewire_events_ingested_total` | Counter | chain, address | Ingestion rate |
| `whalewire_event_lag_seconds` | Gauge | chain, address | Time since last event per address |
| `whalewire_alerts_fired_total` | Counter | asset, direction | Alert count |
| `whalewire_circuit_breaker_state` | Gauge | — | 0=closed, 1=half-open, 2=open |
| `whalewire_dlq_messages_total` | Gauge | queue | DLQ depth (or count) |
| `whalewire_discovery_addresses_total` | Gauge | — | Addresses discovered last cycle |
| `whalewire_discovery_last_success_timestamp_seconds` | Gauge | — | Unix timestamp of last successful discovery (for WhaleWireDiscoveryFailed) |

**Endpoint:** `GET /metrics` (Prometheus format)

---

### 2. Alerting Rules (required, strict) — Done

| Alert | Condition | Severity |
|-------|-----------|----------|
| `WhaleWireDlqMessage` | Any message in DLQ | Critical |
| `WhaleWireCircuitBreakerOpen` | Circuit breaker open | Critical |
| `WhaleWireIngestionStalled` | No events for 10+ minutes | Warning |
| `WhaleWireEventLagHigh` | Lag > 30 minutes for any address | Warning |
| `WhaleWireDiscoveryFailed` | No successful discovery in 2+ hours | Warning |

**Principle:** One DLQ message → alert. No thresholds for DLQ.

**Location:** `prometheus/alerts/whalewire.yml`. Prometheus + Alertmanager in `docker-compose.yml` (ports 9090, 9093).

---

### 3. Correlation IDs (required) — Done

- Add trace/correlation ID to each event flow (ingestion → publish → consume → handler).
- Log it in every relevant log line.
- Enables: "Find all logs for event X" without grep.

**Implementation:** B (in `BlockchainEvent`) + C (RabbitMQ `BasicProperties.CorrelationId`). `ICorrelationIdAccessor` scoped for handler/notifier. Set at publish in `IngestorUseCase`; consumer reads from header or message body.

**Evidence:**
- `IngestorUseCaseTests.ExecuteAsync_WithEvents_PublishesEachEventWithCorrelationId` — publishes events with non-null 32-char CorrelationId
- `BlockchainEventHandlerTests.HandleAsync_LogsCorrelationIdFromAccessor` — handler logs CorrelationId from scoped accessor
- `ConsoleAlertNotifierTests.NotifyAsync_LogsAlertWithWarningLevelAndCorrelationId` — notifier logs CorrelationId in whale alerts
- `AlertEvaluatorTests.EvaluateAsync_InvalidJson_LogsCorrelationId` — evaluator logs CorrelationId on RawJson parse failure
- `CorrelationIdE2ETests.PublishViaRabbitMQ_EventProcessedEndToEnd` — E2E verifies full pipeline (Publisher → RabbitMQ → Consumer → Handler → DB)
- `CorrelationIdHandlerIntegrationTests.HandlerInvokedWithCorrelationId_CorrelationIdAppearsInCapturedLogs` — integration test proves handler logs CorrelationId when accessor is set (real handler, real DB)

---

### 4. Optional

| Item | Purpose |
|------|---------|
| **OpenTelemetry tracing** | Spans across ingestion → handler → alert |
| **Structured logging sink** | JSON logs to Seq/ELK/Loki |
| **Grafana dashboard** | **In docker compose:** Grafana on port 3000, provisioned dashboard *WhaleWire overview* — see [LAUNCH_AND_MONITORING.md](LAUNCH_AND_MONITORING.md) |

---

## Future

| Item | Notes |
|------|-------|
| **Alertmanager receivers** | Alerts show in UI (localhost:9093) but are not delivered. Add `webhook_configs`, `slack_configs`, or `pagerduty_configs` to `prometheus/alertmanager.yml` for Slack, PagerDuty, etc. Environment-specific. |

---

## Implementation Order

1. Prometheus metrics — `/metrics` + the six metrics
2. Alerting rules — Alertmanager config with strict conditions
3. Correlation IDs — Trace ID propagation and logging
4. (Optional) OpenTelemetry, structured logging, Grafana dashboard
