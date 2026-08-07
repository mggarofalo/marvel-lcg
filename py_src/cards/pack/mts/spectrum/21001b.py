from . import *

# * Monica Rambeau

def GetAbilities() -> Sequence['Ability']:

    def put_energy_form_upgrades(effect: 'Effect', faces: List['CardFace']) -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.FlipAllTo(faces, False, effect)


    def monica_rambeau(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.FlipAllTo(initiator.form.FindAllEnergyForms(), False, effect)


    return [
        AbilityFactory.SetupPutIntoPlay([
                "21002",
                "21003",
                "21004"
            ],
            operation=put_energy_form_upgrades
        ).SetName("Power Down"),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "You",
            monica_rambeau,
            to_this_form=True,
        ).SetName("Power Down"),
    ]

