from . import *

# * Avengers Tower

def GetAbilities() -> Sequence['Ability']:

    def avengers_tower(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Faces.RemoveCountersOn([this], "All", 'damage', effect)
        this.card.Flip(effect)

    return [
        AbilityFactory.UniqueRuleNotApplyTo(
            "This"
        ),
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.ForcedResponse,
            "This",
            "damage",
            avengers_tower,
            at_least="9*",
        ),
    ]

