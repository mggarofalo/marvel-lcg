from . import *

# * Ororo Munroe

def GetAbilities() -> Sequence['Ability']:

    def ororo_munroe(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        faces = GetWeatherDeck(effect)
        face = initiator.AskChooseFace(faces, effect)
        if face:
            face.CastTo(Support).PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.BeginGameWithSetAside([
            "36002",
            "36003",
            "36004",
            "36005"
        ]),
        AbilityFactory.WhenCardSetup(
            "This",
            ororo_munroe
        ).SetName('"I feel a storm coming..."'),
    ]

