using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;
public abstract class AbilityDataTestBase
{
    protected static readonly CardCatalog Printed = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    /// <summary>
    /// Every name an ability tree uses: the keys, and a query's value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A query is the one place the vocabulary is in the value rather than the
    /// key — <c>{ "query": "alliesYouControl" }</c> — because a query names a
    /// set of cards and the node names the act of asking.
    /// </para>
    /// <para>
    /// Only the keys, and a query's value. A word in any other value position
    /// is a card id, a keyword the engine already reads, or a trait — all held
    /// against something else already.
    /// </para>
    /// </remarks>
    protected static void Words(JsonElement element, SortedSet<string> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var field in element.EnumerateObject())
                {
                    if (field.Name == "query" && field.Value.ValueKind == JsonValueKind.String)
                    {
                        found.Add(field.Value.GetString()!);
                        continue;
                    }

                    found.Add(field.Name);
                    Words(field.Value, found);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Words(item, found);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>Every trait one effect tree names, however deep.</summary>
    protected static IEnumerable<string> Traits(AbilityNode node) => Traits(node.Argument, node.Kind);
    protected static IEnumerable<string> Traits(AbilityValue value, string kind)
    {
        switch (value)
        {
            case AbilityValue.Word word when string.Equals(kind, "enemiesWithTrait", StringComparison.Ordinal):
                yield return word.Value;
                break;
            case AbilityValue.List list:
                foreach (string found in list.Values.SelectMany(each => Traits(each, kind)))
                {
                    yield return found;
                }

                break;
            case AbilityValue.Map map:
                foreach (var(name, entry)in map.Entries)
                {
                    foreach (string found in Traits(entry, name))
                    {
                        yield return found;
                    }
                }

                break;
            default:
                break;
        }
    }
}
