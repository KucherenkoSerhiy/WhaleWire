#!/bin/bash
# WhaleWire System Health Check
# Run: ./check-system.sh

echo -e "\n========================================"
echo "   WhaleWire System Health Check"
echo -e "========================================\n"

# Check if docker is running
echo "Checking Docker..."
if ! docker ps | grep -q "whalewire-postgres"; then
    echo "ERROR: Docker containers not running!"
    echo "Run: docker compose up -d"
    exit 1
fi
echo -e "✓ Docker containers running\n"

# Execute queries
echo "Running diagnostics..."

docker exec -i whalewire-postgres psql -U whalewire -d whalewire <<'EOF'
\echo '\n===== DISCOVERY STATUS ====='
SELECT 
    asset_id,
    COUNT(*) as addresses,
    COUNT(DISTINCT address) as unique_wallets
FROM monitored_addresses
WHERE is_active = true
GROUP BY asset_id
ORDER BY addresses DESC;

\echo '\n===== MULTI-ASSET WHALES (Top 10) ====='
SELECT 
    LEFT(address, 50) as wallet,
    COUNT(DISTINCT asset_id) as assets,
    STRING_AGG(asset_id, ', ') as asset_list
FROM monitored_addresses
WHERE is_active = true
GROUP BY address
HAVING COUNT(DISTINCT asset_id) > 1
ORDER BY assets DESC
LIMIT 10;

\echo '\n===== EVENTS ====='
SELECT 
    COUNT(*) as total,
    COUNT(DISTINCT address) as addresses_processed,
    COUNT(DISTINCT event_id) as unique_events
FROM events;

\echo '\n===== CHECKPOINTS ====='
SELECT COUNT(*) as checkpointed_addresses FROM checkpoints;

\echo '\n===== SUMMARY ====='
SELECT 
    (SELECT COUNT(*) FROM monitored_addresses WHERE is_active = true) as total_rows,
    (SELECT COUNT(DISTINCT address) FROM monitored_addresses WHERE is_active = true) as unique_wallets,
    (SELECT COUNT(DISTINCT asset_id) FROM monitored_addresses WHERE is_active = true) as assets,
    (SELECT COUNT(*) FROM events) as events,
    (SELECT COUNT(*) FROM checkpoints) as checkpoints;
EOF

echo -e "\n========================================"
echo "   Health Check Complete"
echo -e "========================================\n"
