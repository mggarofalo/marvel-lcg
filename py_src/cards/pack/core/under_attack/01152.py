from . import *

# Vibranium Armor

def GetAbilities() -> Sequence['Ability']:

    def vibranium_armor(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            Villain,
            vibranium_armor
        ).SetTarget("Trigger"),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("RR"))
        .SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]
