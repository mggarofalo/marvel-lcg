from . import *

# Martial Arts Training

def GetAbilities() -> Sequence['Ability']:

    def martial_arts_training(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=1,
        ),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            CardFinder(name="Psylocke"),
            martial_arts_training
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Trigger"),
    ]

