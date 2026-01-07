namespace WhaleWire.Tests.Common.Builders;

public sealed class EventTestDataBuilder
{
    private string _eventId = $"test-event-{Guid.NewGuid()}";
    private string _chain = "ton-testnet";
    private string _address = "EQTestAddress123";
    private long _lt = 1000;
    private string _txHash = "hash123";
    private DateTime _blockTime = DateTime.UtcNow;
    private string _rawJson = """{"test": true}""";

    public EventTestDataBuilder WithEventId(string eventId)
    {
        _eventId = eventId;
        return this;
    }

    public EventTestDataBuilder WithChain(string chain)
    {
        _chain = chain;
        return this;
    }

    public EventTestDataBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public EventTestDataBuilder WithLt(long lt)
    {
        _lt = lt;
        return this;
    }

    public EventTestDataBuilder WithTxHash(string txHash)
    {
        _txHash = txHash;
        return this;
    }

    public EventTestDataBuilder WithBlockTime(DateTime blockTime)
    {
        _blockTime = blockTime;
        return this;
    }

    public EventTestDataBuilder WithRawJson(string rawJson)
    {
        _rawJson = rawJson;
        return this;
    }

    public EventTestData Build() => new(
        _eventId, _chain, _address, _lt, _txHash, _blockTime, _rawJson);
}

public record EventTestData(
    string EventId,
    string Chain,
    string Address,
    long Lt,
    string TxHash,
    DateTime BlockTime,
    string RawJson);