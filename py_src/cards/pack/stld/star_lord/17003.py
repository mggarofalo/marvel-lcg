from . import *

# Daring Escape

def GetAbilities() -> Sequence['Ability']:

    def daring_escape(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        Faces.ReadyAll([hero], effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            daring_escape
        ).SetPlay()
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "Initiator"))
    ]

