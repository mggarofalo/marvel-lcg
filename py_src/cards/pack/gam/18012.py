from . import *

# Clobber

def GetAbilities() -> Sequence['Ability']:

    def clobber(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)
        
        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 3, effect)

        if initiator.stat.IsFirstCardPlayedThisRound():
            Faces.ReturnToHand([this], initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            clobber
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

