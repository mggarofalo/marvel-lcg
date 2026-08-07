from . import *

# Humongous Hallucination

def GetAbilities() -> Sequence['Ability']:

    def humongous_hallucination(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        initiator = effect.GetInitiator()
        ShuffleTopEncounterDeckIntoPlayerDeck(initiator, 2, effect)
        return True


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=MYSTERIO)
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("1"))
        .SetCostFunc(CostFunc.Custom(Select.From("EncounterDeckTop", range=(2, 2)), humongous_hallucination)),
    ]

