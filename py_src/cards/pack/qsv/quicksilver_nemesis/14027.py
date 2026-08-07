from . import *

# Vibration Resistance

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Avalanche"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.ReduceCharacterTakeDamageBy(
            Enemy,
            1,
            this_attached_unit_only=True,
            from_each_attack=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.Exhaust("YourHero"))
    ]

