namespace Marvel.Server;

/// <summary>Reads lines while rejecting individual records above a fixed character bound.</summary>
internal sealed class BoundedLineReader(TextReader reader, int maximumCharacters)
{
    internal bool Read(out string? value)
    {
        var line = new System.Text.StringBuilder();
        bool exceeded = false;
        while (true)
        {
            int character = reader.Read();
            if (character < 0) return End(line, exceeded, out value);
            if (character == '\n') return Complete(line, exceeded, out value);
            if (!exceeded) exceeded = Append(line, (char)character);
        }
    }

    private bool Append(System.Text.StringBuilder line, char character)
    {
        if (line.Length < maximumCharacters)
        {
            line.Append(character);
            return false;
        }
        line.Clear();
        return true;
    }

    private static bool Complete(
        System.Text.StringBuilder line, bool exceeded, out string? value)
    {
        if (!exceeded && line.Length > 0 && line[^1] == '\r') line.Length--;
        value = exceeded ? null : line.ToString();
        return true;
    }

    private static bool End(
        System.Text.StringBuilder line, bool exceeded, out string? value)
    {
        if (!exceeded && line.Length == 0)
        {
            value = null;
            return false;
        }
        value = exceeded ? null : line.ToString();
        return true;
    }
}
