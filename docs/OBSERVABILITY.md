# WhaleWire Observability Plan

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
| Alerting rules | Pending |
| Correlation IDs | Pending |

**Evidence:** Discovery failure test, HTTP-mocked TonCenter test, `/metrics` E2E test (`MetricsEndpointE2ETests`).

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

**Endpoint:** `GET /metrics` (Prometheus format)

---

### 2. Alerting Rules (required, strict)

| Alert | Condition | Severity |
|-------|-----------|----------|
| `WhaleWireDlqMessage` | Any message in DLQ | Critical |
| `WhaleWireCircuitBreakerOpen` | Circuit breaker open | Critical |
| `WhaleWireIngestionStalled` | No events for 10+ minutes | Warning |
| `WhaleWireEventLagHigh` | Lag > 30 minutes for any address | Warning |
| `WhaleWireDiscoveryFailed` | Discovery cycle error | Warning |

**Principle:** One DLQ message → alert. No thresholds for DLQ.

---

### 3. Correlation IDs (required)

- Add trace/correlation ID to each event flow (ingestion → publish → consume → handler).
- Log it in every relevant log line.
- Enables: "Find all logs for event X" without grep.

---

### 4. Optional

| Item | Purpose |
|------|---------|
| **OpenTelemetry tracing** | Spans across ingestion → handler → alert |
| **Structured logging sink** | JSON logs to Seq/ELK/Loki |
| **Grafana dashboard** | Pre-built dashboard for the metrics above |

---

## Implementation Order

1. Prometheus metrics — `/metrics` + the six metrics
2. Alerting rules — Alertmanager config with strict conditions
3. Correlation IDs — Trace ID propagation and logging
4. (Optional) OpenTelemetry, structured logging, Grafana dashboard
