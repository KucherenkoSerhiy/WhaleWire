namespace WhaleWire.Infrastructure.Persistence.Exceptions;

public sealed class CheckpointConflictException(long lt, string existingHash, string newHash)
    : Exception($"Checkpoint conflict: Lt {lt} exists with hash '{existingHash}', cannot update to '{newHash}'")
{
}
