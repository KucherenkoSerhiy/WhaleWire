# Observation: max event lag grows with wall clock (long run)

**Status:** Recorded only — no fix until more data.

**What we see:** `max(whalewire_event_lag_seconds)` rises roughly **1 second per second of real time** for **30+ minutes** (not a short post-restart spike that flattens).

**How detected:** Grafana time series for max lag; compare slope to uptime. Linear growth implies the **worst checkpoint’s `UpdatedAt` is not moving** for that entire window.

**Likely meaning:** **Many** checkpoints can sit above threshold while ingestion/alerts look active — correlate **stale rows vs `monitored_addresses`** (orphans vs real gaps).

**Update:** Dashboards showed **~225 / ~272** stale (15 m threshold) with max lag still climbing ~1:1 with wall time — confirms **systemic** signal, not a single bad row.

**Follow-up when ready:** DB: stalest `(chain, address)`, still monitored?, ingestion errors for those rows.
