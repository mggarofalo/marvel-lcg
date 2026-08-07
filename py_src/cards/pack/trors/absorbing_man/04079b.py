from . import *

# None Shall Pass

def GetAbilities() -> Sequence['Ability']:

    def none_shall_pass(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            card_type=Environment
        )
        for face in faces:
            if face != message.trigger:
                Faces.DiscardAll([face], effect)

    def place_delay(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Faces.PlaceCountersOn([this], 1, 'delay', effect)

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            place_delay
        ),
        AbilityFactory.WhenCardEnterPlay(
            AbilityType.ForcedInterrupt,
            Environment,
            none_shall_pass
        ),
    ]

