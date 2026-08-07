from . import *

# A Minor Setback

def GetAbilities() -> Sequence['Ability']:

    def a_minor_setback(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        identity = player.GetIdentity()
        if Faces.RemoveCountersOn([identity], 1, 'progress', effect):
            Faces.DiscardAll([this], effect)
        else:
            player.DealEncounterCards(1, effect)
            deck = Worlds.GetEncounterDeck(effect)
            Faces.ShuffleAllTo([this], deck, effect)

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            a_minor_setback
        ),
    ]

