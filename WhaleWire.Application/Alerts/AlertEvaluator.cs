using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WhaleWire.Messages;

namespace WhaleWire.Application.Alerts;

public sealed class AlertEvaluator(ILogger<AlertEvaluator> logger) : IAlertEvaluator
{
    private const decimal TonDecimals = 1_000_000_000m; // 9 decimals for TON
    private const decimal ThresholdTon = 100m; // 100 TON threshold

    public Task<IReadOnlyList<Alert>> EvaluateAsync(
        BlockchainEvent blockchainEvent,
        CancellationToken ct = default)
    {
        var alerts = new List<Alert>();

        try
        {
            using var doc = JsonDocument.Parse(blockchainEvent.RawJson);
            var root = doc.RootElement;

            // Check if there's a "transactions" array (TonAPI response format)
            if (root.TryGetProperty("transactions", out var transactionsArray))
            {
                foreach (var tx in transactionsArray.EnumerateArray())
                {
                    alerts.AddRange(EvaluateTransaction(tx, blockchainEvent));
                }
            }
            // Or check if it's a single transaction object (test format)
            else if (root.TryGetProperty("in_msg", out _) || root.TryGetProperty("out_msgs", out _))
            {
                alerts.AddRange(EvaluateTransaction(root, blockchainEvent));
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse RawJson for event {EventId}", blockchainEvent.EventId);
        }

        return Task.FromResult<IReadOnlyList<Alert>>(alerts);
    }

    private List<Alert> EvaluateTransaction(JsonElement tx, BlockchainEvent evt)
    {
        var alerts = new List<Alert>();

        // Check incoming message
        if (tx.TryGetProperty("in_msg", out var inMsg))
        {
            var alert = EvaluateMessage(inMsg, evt, "IN");
            if (alert is not null)
                alerts.Add(alert);
        }

        // Check outgoing messages
        if (tx.TryGetProperty("out_msgs", out var outMsgs))
        {
            foreach (var outMsg in outMsgs.EnumerateArray())
            {
                var alert = EvaluateMessage(outMsg, evt, "OUT");
                if (alert is not null)
                    alerts.Add(alert);
            }
        }

        return alerts;
    }

    private Alert? EvaluateMessage(JsonElement msg, BlockchainEvent evt, string direction)
    {
        // Extract value from message
        if (!msg.TryGetProperty("value", out var valueProp))
            return null;

        var valueStr = valueProp.GetString();
        if (string.IsNullOrEmpty(valueStr))
            return null;

        if (!BigInteger.TryParse(valueStr, out var valueNano))
            return null;

        // Convert to human-readable TON
        var amountTon = (decimal)valueNano / TonDecimals;

        // Check threshold
        if (amountTon < ThresholdTon)
            return null;

        // Extract source/destination
        var source = msg.TryGetProperty("source", out var srcProp) 
            ? srcProp.GetString() ?? "unknown" 
            : "unknown";
        
        var destination = msg.TryGetProperty("destination", out var dstProp) 
            ? dstProp.GetString() ?? "unknown" 
            : "unknown";

        var message = direction == "IN"
            ? $"Received from {FormatAddress(source)}"
            : $"Sent to {FormatAddress(destination)}";

        return new Alert(
            AssetId: "TON",
            WalletAddress: evt.Address,
            Amount: amountTon,
            Direction: direction,
            Message: message);
    }

    private static string FormatAddress(string address)
    {
        return address.Length > 10 
            ? address[..10] + "..." 
            : address;
    }
}
