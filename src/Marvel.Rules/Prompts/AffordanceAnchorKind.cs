namespace Marvel.Rules.Prompts;

/// <summary>The namespace of an affordance anchor identifier.</summary>
/// <remarks>
/// Card and area identifiers are independently allocated. The discriminator is
/// therefore part of the engine's wire contract: a presentation consumer must
/// not infer an anchor's kind by probing either collection.
/// </remarks>
public enum AffordanceAnchorKind
{
#pragma warning disable CS1591, SA1602
    Card,
    Area,
    Unspecified,
#pragma warning restore CS1591, SA1602
}
