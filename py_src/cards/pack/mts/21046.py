from . import *

# Audacity

def GetAbilities() -> Sequence['Ability']:

    def audacity(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.HeroResponse,
            audacity
        ).SetTarget(Villain),
    ]

