from . import *

# Supersonic Punch

def GetAbilities() -> Sequence['Ability']:

    def supersonic_punch(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = 4
        hero = initiator.GetHero()
        if hero.HasTrait("AERIAL"):
            damage = 8
        this.DealDamage(effect.targets, damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            supersonic_punch
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

