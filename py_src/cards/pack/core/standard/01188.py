from . import *

# Caught Off Guard

def GetAbilities() -> Sequence['Ability']:

    def caught_off_guard(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardControlCards(effect, upgrade=True, support=True)
        if face == None:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            caught_off_guard
        )
    ]
