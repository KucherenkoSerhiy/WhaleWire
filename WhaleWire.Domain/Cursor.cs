namespace WhaleWire.Domain;

public sealed record Cursor(long Primary, string Secondary)
{
    public override string ToString() => $"{Primary}:{Secondary}";
    
    public static Cursor Parse(string value)
    {
        return TryParse(value, out var cursor) 
            ? cursor 
            : throw new FormatException($"Invalid cursor format: {value}");
    }

    private static bool TryParse(string? value, out Cursor cursor)
    {
        cursor = null!;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':');
        if (parts.Length != 2)
            return false;

        if (!long.TryParse(parts[0], out var primary))
            return false;
        
        if (string.IsNullOrWhiteSpace(parts[1]))
            return false;

        var secondary = parts[1];

        cursor = new Cursor(primary, secondary);
        
        return true;
    }
}