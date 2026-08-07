from . import *

# Power Gauntlets

def GetAbilities() -> Sequence['Ability']:

    def power_gauntlets_boost(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            "AttachedVillain",
            "You",
            power_gauntlets_boost,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("BR")),
    ]

