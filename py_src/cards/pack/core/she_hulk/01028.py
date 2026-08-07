from . import *

# Superhuman Strength

def GetAbilities() -> Sequence['Ability']:

    def superhuman_strength(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="She-Hulk"),
            attack=2,
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="She-Hulk"),
            superhuman_strength
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Targets", canbe_stunned=True),
    ]

