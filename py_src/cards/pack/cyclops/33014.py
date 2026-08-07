from . import *

# * Blindfold: Ruth Aldine

def GetAbilities() -> Sequence['Ability']:

    def blindfold(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck("EncounterDeck", 5, effect)
        initiator.AskDiscardFace(faces, effect, not_shuffle=True)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            blindfold
        ),
    ]

