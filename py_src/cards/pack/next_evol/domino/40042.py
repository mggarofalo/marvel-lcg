from . import *

# Right Place, Right Time

def GetAbilities() -> Sequence['Ability']:

    def right_place_right_time(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = 3
        face = initiator.DiscardDeckTopCard(effect)
        value += FacesCounter.GetPrintedResourcesIcon([face], initiator.player_deck)

        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            right_place_right_time
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

