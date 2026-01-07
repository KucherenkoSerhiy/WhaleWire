namespace WhaleWire.Tests.Common.Builders;

public static class ObjectMother
{
    public static class Events
    {
        public static EventTestDataBuilder Default() => new();

        public static EventTestDataBuilder WithKnownEventId(string eventId) =>
            new EventTestDataBuilder().WithEventId(eventId);

        public static EventTestDataBuilder ForIdempotencyTest() =>
            new EventTestDataBuilder()
                .WithEventId("idempotency-test-event")
                .WithLt(12345)
                .WithTxHash("idempotency-test-hash");
    }

    public static class Checkpoints
    {
        public static CheckpointTestDataBuilder Default() => new();

        public static CheckpointTestDataBuilder ForAddress(string address) =>
            new CheckpointTestDataBuilder().WithAddress(address);

        public static CheckpointTestDataBuilder WithConsistentLtAndHash(long lt) =>
            new CheckpointTestDataBuilder()
                .WithLastLtAndHash(lt, $"hash-at-lt-{lt}");
    }

    public static class Leases
    {
        public static (string leaseKey, string ownerId) Default() =>
            ("ton:EQDefault", "worker-1");

        public static (string leaseKey, string ownerId) ForAddress(string address) =>
            ($"ton:{address}", "worker-test");
    }
}