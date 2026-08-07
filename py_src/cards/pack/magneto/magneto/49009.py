from . import *

# Metal Shards

def GetAbilities() -> Sequence['Ability']:

    def metal_shards(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action(unit: 'Unit2'):
            initiator = effect.GetInitiator()
            identity = initiator.GetIdentity()
            Faces.GiveStatus([identity], "Tough", effect)

        this.DealDamage(effect.targets, 7, effect, if_this_attack_defeats=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            metal_shards
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

