namespace Marvel.Cards.Dsl;

/// <summary>An authored destination for an identity form change.</summary>
public abstract record AbilityFormDestination
{
    /// <summary>A generic hero or alter-ego form.</summary>
    public sealed record Form(string Name) : AbilityFormDestination;
    /// <summary>A specific printed face of the changing identity.</summary>
    public sealed record Face(string Id) : AbilityFormDestination;
    /// <summary>The other hero face of the current hero identity.</summary>
    public sealed record OtherHero : AbilityFormDestination;
}
