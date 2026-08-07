from . import *

# * Stryfe

def GetAbilities() -> Sequence['Ability']:

    def stryfe(effect: 'Effect', message: 'Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.CancelEffects(effect)
        this.DealDamage([this], 1, effect)


    return [
        AbilityFactory.WhenPlayerWouldPlayCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            CardFinder2("PSIONIC", Event),
            stryfe
        ),
    ]

