from . import *

# * Valkyrie

def GetAbilities() -> Sequence['Ability']:

    def valkyrie(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        damage = 2
        if message.paid_resources.HasColor("Y"):
            damage = 3
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            valkyrie
        ).SetTarget(Minion),
    ]

