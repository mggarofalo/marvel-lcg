from . import *

# Impede

def GetAbilities() -> Sequence['Ability']:

    def impede(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

        if initiator.stat.IsFirstCardPlayedThisRound():
            Faces.ReturnToHand([this], initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            impede
        ).SetPlay().SetLabel('thwart')
        .SetTarget(MainScheme),
    ]

