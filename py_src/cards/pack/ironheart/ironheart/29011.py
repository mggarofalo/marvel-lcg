from . import *

# * Tony Stark A.I.

def GetAbilities() -> Sequence['Ability']:

    def tony_stark_ai(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck(initiator.player_deck, 2, effect)

        face = initiator.AskChooseFace(faces, effect)
        if face:
            Faces.AddToHand([face], initiator, effect)
            faces.remove(face)

        Faces.DiscardAll(faces, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            tony_stark_ai
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

