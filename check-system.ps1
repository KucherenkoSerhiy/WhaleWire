# WhaleWire System Health Check
# Run: .\check-system.ps1

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   WhaleWire System Health Check" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if docker is running
Write-Host "Checking Docker..." -ForegroundColor Yellow
$dockerRunning = docker ps 2>&1 | Select-String "whalewire-postgres"
if (-not $dockerRunning) {
    Write-Host "ERROR: Docker containers not running!" -ForegroundColor Red
    Write-Host "Run: docker compose up -d" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Docker containers running`n" -ForegroundColor Green

# Execute queries
Write-Host "Running diagnostics...`n" -ForegroundColor Yellow

$result = docker exec -i whalewire-postgres psql -U whalewire -d whalewire -f - @"
-- Discovery Status
\echo '===== DISCOVERY STATUS ====='
SELECT 
    asset_id,
    COUNT(*) as addresses,
    COUNT(DISTINCT address) as unique_wallets
FROM monitored_addresses
WHERE is_active = true
GROUP BY asset_id
ORDER BY addresses DESC;

-- Multi-asset whales
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

-- Events
\echo '\n===== EVENTS ====='
SELECT 
    COUNT(*) as total,
    COUNT(DISTINCT address) as addresses_processed,
    COUNT(DISTINCT event_id) as unique_events
FROM events;

-- Checkpoints
\echo '\n===== CHECKPOINTS ====='
SELECT COUNT(*) as checkpointed_addresses FROM checkpoints;

-- Summary
\echo '\n===== SUMMARY ====='
SELECT 
    (SELECT COUNT(*) FROM monitored_addresses WHERE is_active = true) as total_rows,
    (SELECT COUNT(DISTINCT address) FROM monitored_addresses WHERE is_active = true) as unique_wallets,
    (SELECT COUNT(DISTINCT asset_id) FROM monitored_addresses WHERE is_active = true) as assets,
    (SELECT COUNT(*) FROM events) as events,
    (SELECT COUNT(*) FROM checkpoints) as checkpoints;
"@

Write-Host $result

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Health Check Complete" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
