from . import *

# * Heimdall

def GetAbilities() -> Sequence['Ability']:

    def heimdall(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck("EncounterDeck", 3, effect)
        face = initiator.AskDiscardFace(faces, effect, not_shuffle=True)
        if face:
            faces.remove(face)
        initiator.PlaceOnTopInAnyOrder(faces, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            heimdall
        ),
    ]

