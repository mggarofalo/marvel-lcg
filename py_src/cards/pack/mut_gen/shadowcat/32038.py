from . import *

# Phase Strike

def GetAbilities() -> Sequence['Ability']:

    def phase_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.DealDamage(effect.targets , 6, effect)

        if initiator.form.IsInMassForm("Phased"):
            Players.DiscardHeroActionAttachment(initiator, effect.targets, effect, may=True)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            phase_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

