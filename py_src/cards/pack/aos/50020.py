from . import *

# * The Pericles

def GetAbilities() -> Sequence['Ability']:

    def the_pericles(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence['CardFace']):
            for face in targets:
                if CanStatus.IsType(face):
                    status = initiator.DeclareStatusCard(
                        confused=face.CanbeConfused(),
                        tough=face.CanGainTough(),
                        stunned=face.CanbeStunned()
                    )
                    if status:
                        Faces.GiveStatus([face], status, effect)

        action(effect.targets)
        action(effect.targets2)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_pericles
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'supply'))
        .SetTarget("Hero|Villain", canbe_status="Any")
        .SetTarget2("Ally|Minion", canbe_status="Any"),
    ]

