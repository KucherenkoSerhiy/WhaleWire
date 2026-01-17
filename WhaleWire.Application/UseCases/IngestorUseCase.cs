namespace WhaleWire.Application.UseCases;

public sealed class IngestorUseCase : IIngestorUseCase
{
    public Task<int> ExecuteAsync(string address, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}