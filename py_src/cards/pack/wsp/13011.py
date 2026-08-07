from . import *

# * Thor: Jane Foster

def GetAbilities() -> Sequence['Ability']:

    def thor(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        damage = 2
        if message.paid_resources.HasColor("R"):
            damage = 3
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            thor,
        ).SetTarget(Villain),
    ]

