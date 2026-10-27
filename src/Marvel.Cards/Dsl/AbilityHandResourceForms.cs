using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

// The engine chooses a card-level declaration: spending printed hand resources
// is not a triggered Resource ability. Only the currently required hero form is admitted.
internal static class AbilityHandResourceForms
{
    internal static ImmutableDictionary<string, string> Lower(AbilityBook book)
    {
        var forms = book.HandResourceForms?.ToImmutableDictionary(StringComparer.Ordinal)
            ?? ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);
        foreach (var (card, form) in forms.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!book.Authored.Contains(card) || form != "hero")
                throw new AbilityException($"card '{card}' has an unauthored or unsupported handResourceForm");
        }
        return forms;
    }
}
