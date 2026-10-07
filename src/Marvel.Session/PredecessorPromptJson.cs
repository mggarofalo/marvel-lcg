using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Converts predecessor prompt shapes before strict current-record parsing.</summary>
internal static class PredecessorPromptJson
{
    internal static void AddFixedResourceCosts(JsonObject root)
    {
        foreach (JsonObject prompt in Prompts(root))
            SchemaFourPromptJson.AddFixedResourceCosts(prompt);
    }

    private static IEnumerable<JsonObject> Prompts(JsonObject root)
    {
        if (root["current_prompt"] is { } current) yield return current.AsObject();
        foreach (JsonObject step in JournalSteps(root))
            yield return step["prompt"]?.AsObject()
                ?? throw new JsonException("predecessor journal prompt is null");
    }

    internal static void AddCardAnchorKinds(JsonObject root)
    {
        AddCardAnchorKind(root["current_prompt"]?.AsObject());
        foreach (JsonObject record in JournalSteps(root))
        {
            AddCardAnchorKind(record["prompt"]?.AsObject());
            AddCardAnchorKind(record["decision"]?["selector"]?.AsObject());
        }
    }

    private static IEnumerable<JsonObject> JournalSteps(JsonObject root) =>
        (root["units"]?.AsArray() ?? []).SelectMany(unit =>
            unit?["decisions"]?.AsArray() ?? []).Select(step => step?.AsObject()
                ?? throw new JsonException("schema 3 journal step is null"));

    internal static void AddCardAnchorKind(JsonObject? record)
    {
        if (record is null) return;
        if (record.ContainsKey("anchor_id"))
        {
            AddCardAnchorKindToSelector(record);
            return;
        }

        AddCardAnchorKindToAffordances(record);
    }

    private static void AddCardAnchorKindToSelector(JsonObject selector)
    {
        if (selector.ContainsKey("anchor_kind"))
            throw new JsonException("predecessor decision selector has anchor_kind");
        selector["anchor_kind"] = selector["decline"]?.GetValue<bool>() == true
            ? null
            : (int)AffordanceAnchorKind.Card;
    }

    private static void AddCardAnchorKindToAffordances(JsonObject prompt)
    {
        foreach (JsonNode? affordance in prompt["affordances"]?.AsArray() ?? [])
        {
            JsonObject choice = affordance?.AsObject()
                ?? throw new JsonException("schema 3 affordance is null");
            if (choice.ContainsKey("anchor_kind"))
                throw new JsonException("predecessor affordance has anchor_kind");
            choice["anchor_kind"] = (int)AffordanceAnchorKind.Card;
        }
    }

}
