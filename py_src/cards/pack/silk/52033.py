from . import *

# * Spider-Woman: Jessica Drew

def GetAbilities() -> Sequence['Ability']:

    def spider_woman(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            CardFinder2("WEB-WARRIOR", Ally),
            spider_woman
        ).SetTarget(Enemy),
    ]

