# Observation: avoid full user alerting on launch / catch-up

**Status:** Design constraint — defer enabling Twitter (or similar) until product rules exist.

**What we see:** After restart or cold DB, **whale alert counters** can spike to **very high sustained rates** (e.g. hundreds of ops/s in Grafana `rate()`), driven by backlog and many qualifying movements.

**How detected:** Grafana “whale alerts fired rate” panels after service restart; plus reasoning: one notifier call per qualifying alert × large backlog ⇒ **hundreds of outbound posts** if wired to Twitter with no guardrails.

**Constraint:** Do **not** turn on human-facing alerting that fires on every alert during catch-up. Prefer dedupe, rate caps, quiet-until-healthy, or “only new events since go-live” before shipping Twitter.

**Related:** [ADR 0002](../adr/0002-monitoring-and-notifications-follow-ups.md) (Twitter later, structured audit logging).
