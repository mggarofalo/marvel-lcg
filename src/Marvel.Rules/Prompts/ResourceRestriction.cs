namespace Marvel.Rules.Prompts;

/// <summary>The printed kinds of cost that may consume a generated resource.</summary>
public enum ResourceRestriction
{
    /// <summary>No printed spending restriction.</summary>
    None,
    /// <summary>An Arrow event, as printed on Expert Marksman.</summary>
    ArrowEvent,
    /// <summary>An aspect card's resource cost or ability cost, as printed on Finesse.</summary>
    AspectCard,
    /// <summary>An event's resource or ability cost, as printed on Biokinetic Polymer Suit.</summary>
    Event,
    /// <summary>A Preparation card's resource or ability cost, as printed on Black Widow's Gauntlet.</summary>
    PreparationCard,
    /// <summary>An Attack event, as printed on Martial Prowess.</summary>
    AttackEvent,
}
