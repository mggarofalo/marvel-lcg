from . import *

# Telekinetic Attack

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        damage = 7
        overkill = False
        if identity.HasTrait("UNLEASHED"):
            damage += 2
            overkill = True

        this.DealDamage(effect.targets, damage, effect, property=AttackProperty(overkill=overkill))

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            telekinetic_attack
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

