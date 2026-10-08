using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Passive semantic descriptions of the implemented card program.</summary>
public interface ICardPersistentAbilities
{
    /// <summary>Describes supported complete abilities without offering or executing them.</summary>
    PersistentAbilityDescription DescribePersistent(Card card);
}
