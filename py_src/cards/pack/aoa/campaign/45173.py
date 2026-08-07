from . import *

# * Blink: Clarice Ferguson

def GetAbilities() -> Sequence['Ability']:

    def blink(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterCardEnterHand(
            AbilityType.Response,
            "This",
            blink
        ).SetTarget(Villain),
    ]

