from . import *

# Tactical Brilliance

def GetAbilities() -> Sequence['Ability']:

    def tactical_brilliance(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        initiator.GainCard(effect.targets2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tactical_brilliance
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(trait="TACTIC", from_where=["YourDiscardPile"]),
    ]

