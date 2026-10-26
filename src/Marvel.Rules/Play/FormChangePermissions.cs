using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Live absolute prohibitions on changing an identity's form.</summary>
public static class FormChangePermissions
{
    /// <summary>Engine-selected derived modifier for an absolute form-change prohibition.</summary>
    public const string ProhibitedField = "form_change_prohibited";

    /// <summary>Whether this identity may change form, voluntarily or through an effect.</summary>
    public static bool CanChange(World world, Card identity) =>
        StateFields.Modified(world, identity, ProhibitedField, world.Facts, world.Players) <= 0;

    /// <summary>Refuses a prohibited voluntary change before mutating the identity.</summary>
    public static void Require(World world, Card identity)
    {
        // rr:cannot: "The word cannot is absolute, and cannot be countermanded
        // by other abilities or effects."
        if (!CanChange(world, identity))
            throw new RulesNotImplementedException($"card {identity.ObjectId} cannot change form");
    }
}
