from . import *

# * Nova Prime

def GetAbilities() -> Sequence['Ability']:

    def nova_prime(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.DefeatUnits(effect.targets, this, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            nova_prime,
        ).SetTarget(Minion, non_trait="ELITE")
    ]

