from . import *

# * Cloak

def GetAbilities() -> Sequence['Ability']:

    def cloak_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        Utility.PlaceThreatOnOneScheme(player, 2, effect)
        if Worlds.IsCompetitiveMode(effect):
            assert False

    def cloak_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        if Worlds.FindCardOnField(
            effect,
            name="Dagger",
        ):
            this.Reveal(player, effect)
        else:
            Utility.PlaceThreatOnOneScheme(player, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cloak_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            cloak_boost
        ),
    ]

