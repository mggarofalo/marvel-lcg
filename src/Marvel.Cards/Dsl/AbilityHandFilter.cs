namespace Marvel.Cards.Dsl;

/// <summary>The one printed property a hand-discard cost requires of each discarded card.</summary>
public abstract record AbilityHandFilter
{
    private AbilityHandFilter() { }

    /// <summary>The card prints this resource type.</summary>
    public sealed record PrintedResource(char Resource) : AbilityHandFilter;

    /// <summary>The card prints this classification.</summary>
    public sealed record PrintedClass(string Class) : AbilityHandFilter;
}
