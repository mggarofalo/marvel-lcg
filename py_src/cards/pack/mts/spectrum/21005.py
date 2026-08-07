from . import *

# * Blue Marvel: Adam Brashear

def GetAbilities() -> Sequence['Ability']:

    def blue_marvel(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeEnergyForm(effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            blue_marvel
        ),
    ]

