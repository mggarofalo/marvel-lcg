from . import *

# Gutsy Move

def GetAbilities() -> Sequence['Ability']:

    def gutsy_move(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 2 + initiator.dealt_encounter_cards.GetSize() * 2
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            gutsy_move
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

