using System.Text.RegularExpressions;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content;

/// <summary>Reads nemesis minion roles from complete printed encounter sets.</summary>
internal static class NemesisMinionCatalog
{
    internal static bool Contains(CardCatalogEntry entry, IEnumerable<CardCatalogEntry> cards)
    {
        if (entry.Kind != CardKind.Minion || !entry.Attributes.ContainsKey("Nemesis"))
            return false;
        var minions = cards.Where(card => card.Kind == CardKind.Minion
            && card.Pack == entry.Pack && card.Set == entry.Set).ToArray();
        // rr:nemesis-encounter-set.3: the minion belonging to the set is its
        // nemesis minion; .3.1 designates it by parenthetical text for multiple minions.
        if (minions.Length == 1) return true;
        if (!minions.Any(Designated))
            throw new RulesNotImplementedException(
                $"nemesis set '{entry.Set}' has multiple minions without a printed designation");
        return Designated(entry);
    }

    private static bool Designated(CardCatalogEntry entry) => Regex.IsMatch(entry.Text,
        @"\([^()\r\n]+['’]s nemesis minion\.\)", RegexOptions.CultureInvariant);
}
