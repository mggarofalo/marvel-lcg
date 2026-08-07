from . import *

# Everyday Hero

def GetAbilities() -> Sequence['Ability']:

    def everyday_hero(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.Response,
            everyday_hero,
            conditions=[
                lambda effect, message:
                    effect.this.GetOwnerPlayer().GetIdentity().HasTrait("CIVILIAN")
            ]
        ).SetTarget("ToPlayer", canbe_heal=True),
        AbilityFactory.CheckThisCanDropPay(
        ).AnyPlayerCanDoThis(owner_has_trait="CIVILIAN"),
    ]

