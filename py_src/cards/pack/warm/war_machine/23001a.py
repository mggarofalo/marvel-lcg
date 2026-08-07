from . import *

# * War Machine

def GetAbilities() -> Sequence['Ability']:

    def war_machine(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.PlaceCountersOn([identity], 5, 'ammo', effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            war_machine,
            to_this_form=True
        ).SetName("Locked and Loaded"),
    ]

