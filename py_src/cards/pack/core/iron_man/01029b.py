from . import *

# * Tony Stark

def GetAbilities() -> Sequence['Ability']:

    def tony_stark(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck(initiator.player_deck, 3, effect)
        face = initiator.AskChooseFace(faces, effect)
        if face:
            faces.remove(face)
            Faces.AddToHand([face], initiator, effect)
        Faces.DiscardAll(faces, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            tony_stark
        ).SetName("Futurist")
        .LimitOncePerRound()
    ]

