from . import *

# Innovation

def GetAbilities() -> Sequence['Ability']:

    def innovation(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            innovation
        ).SetTarget("YourAlly", canbe_heal=True),
    ]

