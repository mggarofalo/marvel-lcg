from . import *

# Pinpoint Strike

def GetAbilities() -> Sequence['Ability']:

    def pinpoint_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = 7
        overkill = False

        if initiator.IsInHeroForm('TINY'):
            damage += 1
            overkill = True

        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            pinpoint_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

