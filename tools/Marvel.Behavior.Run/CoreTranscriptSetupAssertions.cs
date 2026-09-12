using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Behavior.Run;

internal static class CoreTranscriptSetupAssertions
{
    internal static void SetupAuthority(
        TranscriptContext context, TranscriptStep step, Match match)
    {
        string authority = match.Groups["authority"].Value;
        string[] parts = authority.Split(':');
        CanonicalCoreScene scene = context.SceneRequired(step);
        switch (parts[1])
        {
            case "campaign":
                AssertCampaignSetup(context, scene, parts[2], step);
                return;
            case "hero":
                AssertHeroSetup(context, scene, parts[2], step);
                return;
            case "encounter-set":
                AssertEncounterSetSetup(context, scene, parts[2], step);
                return;
            default:
                throw new TranscriptAssertionException(
                    $"{step.Location}: unknown setup authority '{authority}'");
        }
    }

    internal static void AssertCampaignSetup(
        TranscriptContext context,
        CanonicalCoreScene scene,
        string name,
        TranscriptStep step)
    {
        if (!string.Equals(scene.Request.Campaign, name, StringComparison.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: scene campaign is '{scene.Request.Campaign}', not '{name}'");
        }

        CampaignSetup campaign = context.Setup.Campaign(name);
        if (campaign.Expert != context.World.Expert)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: expected expert setup mode {campaign.Expert}; "
                + $"was {context.World.Expert}");
        }
        AssertFaceInventory(context.World, campaign.Villain, step, "villain stages");
        AssertFaceInventory(context.World, campaign.Schemes, step, "main schemes");
        AssertFaceInventory(context.World, campaign.SetAside, step, "set-aside cards");
        AssertFaceInventory(context.World, campaign.Encounters, step, "scenario cards");
        foreach (string setName in campaign.EncounterSets)
        {
            AssertFaceInventory(
                context.World, context.Setup.EncounterSet(setName), step,
                $"'{setName}' encounter set");
        }

        IReadOnlyList<string> modular = scene.Request.ModularSets ?? campaign.ModularSets;
        foreach (string setName in modular)
        {
            AssertFaceInventory(
                context.World, context.Setup.EncounterSet(setName), step,
                $"'{setName}' modular set");
        }
    }

    internal static void AssertHeroSetup(
        TranscriptContext context,
        CanonicalCoreScene scene,
        string name,
        TranscriptStep step)
    {
        int seat = scene.Request.Heroes
            .Select((hero, index) => (hero, index))
            .Where(candidate => string.Equals(candidate.hero, name, StringComparison.Ordinal))
            .Select(candidate => candidate.index)
            .DefaultIfEmpty(-1)
            .Single();
        if (seat < 0)
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: scene does not contain hero '{name}'");
        }

        HeroSetup hero = context.Setup.Hero(name);
        AssertOwnedFaceInventory(
            context.World, seat, [.. hero.Hero, .. hero.HeroDeck, .. hero.PlayerDeck],
            step, $"'{name}' owned cards");
        AssertFaceInventory(
            context.World, hero.Obligations,
            step, $"'{name}' obligations");
        AssertAreaFaceInventory(
            context.World.Seats[seat].Nemesis,
            hero.NemesisSet,
            step, $"'{name}' nemesis cards");
    }

    internal static void AssertEncounterSetSetup(
        TranscriptContext context,
        CanonicalCoreScene scene,
        string name,
        TranscriptStep step)
    {
        CampaignSetup campaign = context.Setup.Campaign(scene.Request.Campaign);
        IReadOnlyList<string> selected =
        [
            .. campaign.EncounterSets,
            .. scene.Request.ModularSets ?? campaign.ModularSets,
        ];
        if (!selected.Contains(name, StringComparer.Ordinal))
        {
            throw new TranscriptAssertionException(
                $"{step.Location}: encounter set '{name}' is not selected");
        }

        AssertFaceInventory(
            context.World, context.Setup.EncounterSet(name), step,
            $"'{name}' encounter set");
    }

    internal static void AssertFaceInventory(
        World world,
        IEnumerable<string> expected,
        TranscriptStep step,
        string label)
    {
        var expectedCounts = PhysicalFaces(expected);
        foreach ((string face, int count) in expectedCounts)
        {
            int actual = world.Cards.Count(card =>
                card.Owner == World.Scenario
                && card.Faces.Contains(face, StringComparer.Ordinal));
            Equal(count, actual, $"copies of {face} among {label}", step);
        }
    }

    internal static void AssertOwnedFaceInventory(
        World world,
        int seat,
        IEnumerable<string> expected,
        TranscriptStep step,
        string label)
    {
        foreach ((string face, int count) in PhysicalFaces(expected))
        {
            int actual = world.Cards.Count(card =>
                card.Owner == seat && card.Faces.Contains(face, StringComparer.Ordinal));
            Equal(count, actual, $"copies of {face} among {label}", step);
        }
    }

    internal static void AssertAreaFaceInventory(
        Area area,
        IEnumerable<string> expected,
        TranscriptStep step,
        string label)
    {
        foreach ((string face, int count) in PhysicalFaces(expected))
        {
            int actual = area.Cards.Count(card =>
                card.Faces.Contains(face, StringComparer.Ordinal));
            Equal(count, actual, $"copies of {face} among {label}", step);
        }
    }

    internal static Dictionary<string, int> PhysicalFaces(
        IEnumerable<string> entries) => entries
        .Select(entry => entry.Split(',')[0])
        .GroupBy(face => face, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

}
