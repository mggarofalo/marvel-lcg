from . import *

# * Vision

def GetAbilities() -> Sequence['Ability']:

    def vision(effect: 'Effect', faces: List['CardFace']) -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        # initiator = effect.GetInitiator()
        # initiator.form.SetFromChangeFunc(MassForm.GetPlayerMassForm)

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Condition.PlayerInMassForm("You", "Dense", effect),
            recover=+2,
            change_on_event=OnEvent.Form("YourIdentity")
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                Condition.PlayerInMassForm("You", "Intangible", effect),
            hand_size=+1,
            change_on_event=OnEvent.Form("YourIdentity")
        ),
        AbilityFactory.SetupPutIntoPlay(
            ["26002a,26002b"],
            flip_to_name="Intangible",
            operation=vision
        ),
    ]

