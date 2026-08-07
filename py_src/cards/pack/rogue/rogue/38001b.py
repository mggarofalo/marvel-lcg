from . import *

# * Anna Marie

def GetAbilities() -> Sequence['Ability']:

    def anna_marie(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        touched = FindTouched(effect)
        if touched:
            Faces.SetAside([touched], effect)

    return [
        AbilityFactory.SetupWithSetAside(["38002"]),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            anna_marie,
            to_this_form=True,
        ).SetName("Withdrawn"),
    ]

