from . import *

# * Melinda May

def GetAbilities() -> Sequence['Ability']:

    def melinda_may(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = initiator.LookAtDeck("EncounterDeck", 1, effect)
        initiator.AskDiscardFaces(faces, "Any", effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            melinda_may
        ),
    ]

