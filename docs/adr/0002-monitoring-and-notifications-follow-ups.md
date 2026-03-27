# ADR 0002: Monitoring and notifications follow-ups

## Status

Proposed / working notes — not required reading for the core observability model (see [ADR 0001](0001-observability-principles.md)).

## Context

ADR 0001 sets principles and where each signal class belongs. This document tracks **concrete follow-ups** before or while implementing them.

## Checkpoint lag scope and SLOs

`max(whalewire_event_lag_seconds)` is the **worst** checkpoint staleness in the data the collector sees. That is honest for “something is very old” but one cold or orphan row can dominate the chart.

| Approach | Meaning |
|----------|---------|
| **Global (current)** | Lag over all checkpoints returned by the repository — simple, surfaces orphans |
| **Monitored set only** | Lag only for rows tied to current `monitored_addresses` (or equivalent) — closer to “what we promise to poll” |
| **Dual series** | Keep global max **and** add e.g. max or p95 over monitored-only or “updated in last 24h” — SLO-friendly without hiding orphans |

Pragmatic path: keep **global** as the primary “reality” series; add **dual** when dashboards need both “worst anywhere” and “worst among wallets we care about now.”

Record the implemented choice in code comments or here when done.

## Structured logging for whale decisions

**Goal:** one machine-readable record per **outcome** (sent, suppressed, failed) for support and tuning, without parsing console text.

**Shape to finalize in implementation:**

- When: UTC timestamp, stable `category` (e.g. `whale_decision`).
- Traceability: `correlation_id`, `event_id` when present.
- Business: address, chain, direction, notional, asset.
- Outcome: `sent` | `suppressed` | `failed` plus a small `reason_code` set (`below_threshold`, `dedupe`, `quiet_hours`, `rate_limited`, `notifier_error`, …).
- Channel: e.g. `console` until another notifier ships.

Prefer **one JSON object per line** (JSONL) for later Loki/ELK. Log level: at least **info** for sent; suppressed can be **debug** or **info** depending on volume after caps.

## Twitter as the human notifier (later)

Defer until checkpoint/logging work above is in good shape.

- Implement behind the existing notifier abstraction so domain/application stay free of Twitter types.
- Configuration isolated under something like `Twitter:*` in `WhaleWire.Infrastructure.Notifications` (or a small sibling module).
- Revisit this section when the integration starts; optionally supersede with a focused ADR for credentials and posting rules.

## References

- [ADR 0001 — Observability principles](0001-observability-principles.md)
- [OBSERVABILITY.md](../OBSERVABILITY.md)
