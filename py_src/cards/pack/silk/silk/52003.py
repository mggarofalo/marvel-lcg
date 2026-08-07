from . import *

# Swinging Silk Kick

def GetAbilities() -> Sequence['Ability']:

    def swinging_silk_kick(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        damage = 7
        overkill = False
        if YouMayDiscardACardTuckedUnderSilkFromTheSameSet(initiator, effect.targets, effect):
            damage +=2
            overkill = True

        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(overkill=overkill))

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swinging_silk_kick
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

