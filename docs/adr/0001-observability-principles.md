# ADR 0001: Observability principles and signal placement

## Status

Accepted

## Context

WhaleWire is a **pipeline** (ingestion, messaging, persistence) and a **product** (whale notifications). Words like “alert” and “lag” mean different things for ops vs users. This ADR fixes **how we think about signals** so Grafana, Prometheus, and logging stay coherent.

## Decision

### Principles

1. **Metrics = ground truth for automation** — Definitions must be clear even when charts look bad. A large max lag is correct if it means “at least one checkpoint in scope has not been updated for a long time.” Dashboards should **state that definition**.
2. **Alerts = few “someone must look” signals** — High signal, right audience. Not every graph becomes a page.
3. **Logs = narrative and audit** — Structured, searchable, with retention. Logs complement metrics; they do not replace SLO counters.
4. **Whale notifications = product channel** — Thresholds, caps, dedupe, and quiet hours are **product rules**, not the same as Prometheus firing on DLQ or stalled ingestion.

### What lives where

| Concern | Metrics | Alerts (audience / intent) | Logs |
|--------|---------|----------------------------|------|
| **Pipeline health** (ingest, consume, DB, RabbitMQ) | Rates, errors, DLQ depth, circuit breaker, optional queue depth | **Ops / on-call:** DLQ, circuit breaker open, ingestion stalled, discovery stale (`prometheus/alerts/whalewire.yml`) | Errors, retries, rejects; correlation id where useful |
| **Coverage / freshness** | Checkpoint-based lag (`now - checkpoint.UpdatedAt` per chain/address); optional later: monitored count, checkpoints touched in a recent window | **Ops (warning):** high lag when policy says those addresses should be hot — wording should say **stale checkpoint(s)** | Per-address detail at debug or sampled |
| **Whale signal** (business) | e.g. `whalewire_alerts_fired_total`; optional later: suppressed counts, histograms | **End users** via notifiers: only after **product rules**, not raw metric spikes | **Canonical structured line** per deliver vs suppress, with reason and ids |
| **Prometheus / Alertmanager** | Scrape health, rule evaluation | **Ops:** target down, receiver failures | Optional: webhook to durable store |

What is implemented today: [OBSERVABILITY.md](../OBSERVABILITY.md), [LAUNCH_AND_MONITORING.md](../LAUNCH_AND_MONITORING.md). This ADR is **intent**; those docs are **inventory**.

### Business decisions vs this ADR

- Here we record **engineering alignment**: roles of metrics, alerts, and logs, and the **boundary** between pipeline health and product notifications.
- **Commercial thresholds** (minimum notionals, daily caps, quiet hours, which wallets count) are **business decisions**. Lock them in a product note or a separate ADR when ready.

## Consequences

- New metrics and rules should fit the table (e.g. do not page ops on whale counters unless that is an explicit product choice).
- Grafana titles and descriptions should name **metric semantics** (especially lag), not vague labels like “latency.”

## References

- [OBSERVABILITY.md](../OBSERVABILITY.md)
- [LAUNCH_AND_MONITORING.md](../LAUNCH_AND_MONITORING.md)
- `prometheus/alerts/whalewire.yml`
- [ADR 0002](0002-monitoring-and-notifications-follow-ups.md) — checkpoint lag scope, whale audit logs, notifier plans
