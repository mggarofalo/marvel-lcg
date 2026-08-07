from . import *

# Preservation

def GetAbilities() -> Sequence['Ability']:

    def preservation(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            preservation
        ).SetTarget("YourHero", canbe_heal=True),
    ]

