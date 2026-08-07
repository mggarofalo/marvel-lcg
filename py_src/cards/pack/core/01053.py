from . import *

# Relentless Assault

def GetAbilities() -> Sequence['Ability']:

    def relentless_ssault(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        overkill = effect.GetPaidResources().HasColor("R")
        hero = initiator.GetHero()
        hero.DealDamage(effect.targets, 5, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            relentless_ssault
        ).SetPlay().SetLabel('attack')
        .SetTarget(Minion)
    ]

