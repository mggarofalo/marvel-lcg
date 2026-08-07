from . import *

# * Peter Porker

def GetAbilities() -> Sequence['Ability']:

    def peter_porker(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'toon', effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            peter_porker,
            powers=["REC"],
        ).SetName("Cartoon Power"),
    ]

