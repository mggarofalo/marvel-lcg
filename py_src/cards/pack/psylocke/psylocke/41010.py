from . import *

# Psionic Training

def GetAbilities() -> Sequence['Ability']:

    def psionic_training(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.UnitIgnoreKeywordIcons(
            CardFinder(name="Psylocke"),
            guard=True,
            patrol=True,
        ),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            CardFinder(name="Psylocke"),
            psionic_training
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Enemy, canbe_confused=True),
    ]

