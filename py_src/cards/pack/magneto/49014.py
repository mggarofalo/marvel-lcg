from . import *

# * Phoenix: Jean Grey

def GetAbilities() -> Sequence['Ability']:

    def phoenix(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            phoenix
        ).SetTarget(
            CardFinder(trait="X-MEN", card_type=Ally, canbe_heal=True)|
            CardFinder(trait="X-MEN", card_type=Ally, canbe_ready=True)
        ),
    ]

