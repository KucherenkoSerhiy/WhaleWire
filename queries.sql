-- WhaleWire Diagnostic Queries
-- Run with: docker exec -it whalewire-postgres psql -U whalewire -d whalewire -f queries.sql

\echo '\n===== DISCOVERY STATUS ====='

-- Total addresses per asset
SELECT 
    asset_id,
    COUNT(*) as total_addresses,
    COUNT(DISTINCT address) as unique_addresses
FROM monitored_addresses
WHERE is_active = true
GROUP BY asset_id
ORDER BY total_addresses DESC;

-- Multi-asset whales (same address, multiple assets)
SELECT 
    address,
    COUNT(DISTINCT asset_id) as asset_count,
    STRING_AGG(asset_id, ', ' ORDER BY asset_id) as assets
FROM monitored_addresses
WHERE is_active = true
GROUP BY address
HAVING COUNT(DISTINCT asset_id) > 1
ORDER BY asset_count DESC
LIMIT 10;

-- Top balances per asset
SELECT asset_id, address, LEFT(balance, 25) as balance
FROM monitored_addresses
WHERE is_active = true
ORDER BY asset_id, LENGTH(balance) DESC, balance DESC
LIMIT 15;

\echo '\n===== INGESTION PROGRESS ====='

-- Events summary
SELECT 
    COUNT(*) as total_events,
    COUNT(DISTINCT address) as addresses_with_events,
    COUNT(DISTINCT event_id) as unique_event_ids,
    MIN(created_at) as first_event,
    MAX(created_at) as last_event
FROM events;

-- Events per address (top 10)
SELECT address, COUNT(*) as event_count
FROM events
GROUP BY address
ORDER BY event_count DESC
LIMIT 10;

-- Addresses NOT yet processed
SELECT 
    m.address,
    m.asset_id,
    LEFT(m.balance, 20) as balance
FROM monitored_addresses m
LEFT JOIN events e ON m.address = e.address
WHERE m.is_active = true
GROUP BY m.address, m.asset_id, m.balance
HAVING COUNT(e.id) = 0
LIMIT 10;

\echo '\n===== CHECKPOINTS ====='

-- Checkpoint progress
SELECT 
    COUNT(*) as total_checkpoints,
    COUNT(DISTINCT (chain, address, provider)) as unique_addresses
FROM checkpoints;

-- Latest checkpoints
SELECT chain, LEFT(address, 20) as address, last_lt, last_hash, updated_at
FROM checkpoints
ORDER BY updated_at DESC
LIMIT 10;

-- Addresses with vs without checkpoints
SELECT 
    (SELECT COUNT(DISTINCT address) FROM monitored_addresses WHERE is_active = true) as total_addresses,
    (SELECT COUNT(*) FROM checkpoints) as checkpointed_addresses,
    (SELECT COUNT(DISTINCT address) FROM monitored_addresses WHERE is_active = true) - 
    (SELECT COUNT(*) FROM checkpoints) as not_checkpointed;

\echo '\n===== SYSTEM HEALTH ====='

-- Active leases
SELECT lease_key, owner_id, expires_at
FROM address_leases
WHERE expires_at > NOW()
ORDER BY expires_at DESC
LIMIT 5;

-- Idempotency check (should be 0)
SELECT event_id, COUNT(*) as duplicates
FROM events
GROUP BY event_id
HAVING COUNT(*) > 1
LIMIT 10;

-- Discovery freshness
SELECT 
    asset_id,
    MIN(discovered_at) as oldest_discovery,
    MAX(discovered_at) as newest_discovery
FROM monitored_addresses
WHERE is_active = true
GROUP BY asset_id;

\echo '\n===== SUMMARY ====='

-- Overall system summary
SELECT 
    'Monitored Addresses' as metric,
    COUNT(*)::text as value
FROM monitored_addresses
WHERE is_active = true
UNION ALL
SELECT 
    'Unique Wallets',
    COUNT(DISTINCT address)::text
FROM monitored_addresses
WHERE is_active = true
UNION ALL
SELECT 
    'Assets Tracked',
    COUNT(DISTINCT asset_id)::text
FROM monitored_addresses
WHERE is_active = true
UNION ALL
SELECT 
    'Total Events',
    COUNT(*)::text
FROM events
UNION ALL
SELECT 
    'Addresses Checkpointed',
    COUNT(*)::text
FROM checkpoints;
