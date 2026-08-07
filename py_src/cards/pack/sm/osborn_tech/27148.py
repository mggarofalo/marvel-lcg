from . import *

# Ionic Boots

def GetAbilities() -> Sequence['Ability']:

    def ionic_boots(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            "YourIdentity",
            ionic_boots
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
    ]

