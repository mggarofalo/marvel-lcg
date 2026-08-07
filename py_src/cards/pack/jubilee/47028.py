from . import *

# Mutant Mayhem

def GetAbilities() -> Sequence['Ability']:

    def mutant_mayhem(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.ReturnToHand).return_to_hand_cards
        for face in faces:
            if Ally.IsType(face):
                initiator = face.GetOwnerPlayer()
                initiator.PlayAnAlly(face, False)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mutant_mayhem
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.ReturnToHand(Select.Alliance(["X-FORCE", "X-MEN"], Ally), to_who="Owner")),
    ]

