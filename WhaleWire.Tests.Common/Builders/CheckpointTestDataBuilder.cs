namespace WhaleWire.Tests.Common.Builders;

public sealed class CheckpointTestDataBuilder
{
    private string _chain = "ton-testnet";
    private string _address = "EQTestAddress";
    private string _provider = "tonapi";
    private long _lastLt = 5000;
    private string _lastHash = "checkpoint-hash";

    public CheckpointTestDataBuilder WithChain(string chain)
    {
        _chain = chain;
        return this;
    }

    public CheckpointTestDataBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public CheckpointTestDataBuilder WithProvider(string provider)
    {
        _provider = provider;
        return this;
    }

    public CheckpointTestDataBuilder WithLastLt(long lastLt)
    {
        _lastLt = lastLt;
        return this;
    }

    public CheckpointTestDataBuilder WithLastHash(string lastHash)
    {
        _lastHash = lastHash;
        return this;
    }

    // Build with explicitly related values
    public CheckpointTestDataBuilder WithLastLtAndHash(long lt, string hash)
    {
        _lastLt = lt;
        _lastHash = hash;
        return this;
    }

    public CheckpointTestData Build() => new(
        _chain, _address, _provider, _lastLt, _lastHash);
}

public record CheckpointTestData(
    string Chain,
    string Address,
    string Provider,
    long LastLt,
    string LastHash);