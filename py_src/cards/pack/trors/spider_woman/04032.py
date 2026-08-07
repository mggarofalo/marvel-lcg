from . import *

# * Captain Marvel: Carol Danvers

def GetAbilities() -> Sequence['Ability']:

    def captain_marvel(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            captain_marvel
        ),
    ]

