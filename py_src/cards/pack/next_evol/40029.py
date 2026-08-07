from . import *

# * Psimitar

def GetAbilities() -> Sequence['Ability']:

    def psimitar(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("PSIONIC"),
            psimitar,
            conditions=[
                lambda effect, message:
                    effect.this != message.played_face
            ],
        ).SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]

