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

**Implemented (baseline):** `IWhaleDecisionAuditLogger` + `WhaleDecisionRecord` in `WhaleWire.Application`, `WhaleDecisionAuditLogger` logs one camelCase JSON line per call at **Information** (`{WhaleDecision}`). Outcomes: **`sent`** (`ConsoleAlertNotifier`, channel `console`), **`suppressed`** (`no_qualifying_transfer` when a new event yields zero alerts), **`failed`** (`raw_json_parse_error` on `RawJson` parse failure). Stable reason codes: `WhaleDecisionReasonCodes`. `IAlertEvaluator` returns `AlertEvaluationResult` so parse failures do not also emit a suppressed line.

**Extensions later:** dedupe / quiet hours / rate limit reason codes; optional **Debug** for high-volume suppress paths.

## Twitter as the human notifier (later)

Defer until checkpoint/logging work above is in good shape.

- Implement behind the existing notifier abstraction so domain/application stay free of Twitter types.
- Configuration isolated under something like `Twitter:*` in `WhaleWire.Infrastructure.Notifications` (or a small sibling module).
- Revisit this section when the integration starts; optionally supersede with a focused ADR for credentials and posting rules.

## References

- [ADR 0001 — Observability principles](0001-observability-principles.md)
- [OBSERVABILITY.md](../OBSERVABILITY.md)
