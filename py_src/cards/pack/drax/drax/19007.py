from . import *

# Payback

def GetAbilities() -> Sequence['Ability']:

    def payback(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        player = message.GetToPlayer()
        value = player.GetHero().attack
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.HeroResponse,
            Villain,
            "You",
            payback
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain)
    ]

