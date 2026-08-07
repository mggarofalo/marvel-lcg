from . import *

# * Avengers Tower

def GetAbilities() -> Sequence['Ability']:

    def avengers_tower_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.DiscardEachOther(
            effect,
            this,
            name="Avengers Tower"
        )

    def avengers_tower(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            avengers_tower_revealed
        ),
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.ForcedResponse,
            "This",
            "damage",
            avengers_tower,
            at_least="9*"
        ),
    ]

