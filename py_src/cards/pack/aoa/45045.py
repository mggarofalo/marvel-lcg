from . import *

# Full-Body Charge

def GetAbilities() -> Sequence['Ability']:

    def full_body_charge(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        overkill = hero.health < hero.starting_health / 2
        this.DealDamage(effect.targets, 8, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            full_body_charge
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

