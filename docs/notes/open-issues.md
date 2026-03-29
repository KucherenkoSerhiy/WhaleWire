# Open issues (short list)

| Item | Status | Detail |
|------|--------|--------|
| Checkpoint lag / many wallets stale | Data / fix TBD | [obs-max-lag-linear-growth.md](obs-max-lag-linear-growth.md) — `whalewire_event_lag_stale_wallets` + max lag show **breadth**, not a single orphan. |
| User alerts on catch-up | Constraint | [obs-launch-notification-flood.md](obs-launch-notification-flood.md) |
| Grafana duration labels | Optional polish | Same metrics (seconds); unify panel units/titles so cards don’t mix “hours” vs “mins” awkwardly. |
| RabbitMQ compose readiness | Done | `docker-compose`: healthcheck `rabbitmqctl await_startup` (stricter than ping). App retry still applies. |
