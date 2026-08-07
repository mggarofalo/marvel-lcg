from . import *

# Advance

def GetAbilities() -> Sequence['Ability']:

    def advance(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoSchemes(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            advance
        )
    ]
