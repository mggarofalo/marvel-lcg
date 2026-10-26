namespace Marvel.Content;

/// <summary>Reads explicit printed prerequisites from canonical plain card text.</summary>
internal static class PrintedPlayRestrictions
{
    internal static string? ControlledCharacterTrait(string printed)
    {
        const string prefix = "Play only if you control a ";
        const string suffix = " character";
        foreach (string sentence in printed.Split(['.', '\n']))
        {
            string text = sentence.Trim();
            if (text.StartsWith(prefix, StringComparison.Ordinal)
                && text.EndsWith(suffix, StringComparison.Ordinal)
                && text.Length > prefix.Length + suffix.Length)
                return CardCatalog.TraitKey(text[prefix.Length..^suffix.Length].ToUpperInvariant());
        }
        return null;
    }

    internal static string? IdentityTrait(string printed)
    {
        const string prefix = "Play only if your identity has the ";
        const string suffix = " trait";
        foreach (string sentence in printed.Split(['.', '\n']))
        {
            string text = sentence.Trim();
            if (text.StartsWith(prefix, StringComparison.Ordinal)
                && text.EndsWith(suffix, StringComparison.Ordinal)
                && text.Length > prefix.Length + suffix.Length)
                return CardCatalog.TraitKey(text[prefix.Length..^suffix.Length].ToUpperInvariant());
        }
        return null;
    }
    internal static string? Form(string? printed)
    {
        if (printed is null)
        {
            return null;
        }

        const string suffix = " form only";
        foreach (string line in printed.Split('\n'))
        {
            foreach (string sentence in line.Split('.'))
            {
                string trimmed = sentence.Trim();
                if (!trimmed.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                string form = trimmed[..^suffix.Length];
                if (form.Length > 0 && char.IsUpper(form[0]))
                {
                    return form.ToLowerInvariant();
                }
            }
        }

        return null;
    }

}
