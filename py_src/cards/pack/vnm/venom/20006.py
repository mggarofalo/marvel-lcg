from . import *

# Savage Attack

def GetAbilities() -> Sequence['Ability']:

    def savage_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        overkill = effect.GetPaidResources().OnlyHasColor("Y")
        hero.DealDamage(effect.targets, 5, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            savage_attack
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

