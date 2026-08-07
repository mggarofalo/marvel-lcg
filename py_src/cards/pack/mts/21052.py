from . import *

# Determination

def GetAbilities() -> Sequence['Ability']:

    def determination(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            determination
        ).SetTarget(MainScheme),
    ]

