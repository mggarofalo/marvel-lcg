from . import *

# * Maria Hill

def GetAbilities() -> Sequence['Ability']:

    def maria_hill(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(value: int):
            upgrade = initiator.form.FindSuitForm()
            if upgrade:
                Faces.PlaceTokensOn([upgrade], value, 'threat', effect)

        message.AfterThreatRemoved(action)


    return [
        AbilityFactory.WhenUnitWouldThwart(
            AbilityType.Interrupt,
            "This",
            maria_hill
        ),
    ]

