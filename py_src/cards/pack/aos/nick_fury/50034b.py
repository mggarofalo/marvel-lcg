from . import *

# * Nick Fury

def GetAbilities() -> Sequence['Ability']:

    def nick_fury(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeSuitForm(effect, "Stealth")


    return [
        AbilityFactory.SetupPutIntoPlay(
            ["50035a,50035b"],
            flip_to_name="Assault"
        ).SetName("Suit Up"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            nick_fury
        ).SetName("Infiltrate")
        .SetTarget("YourSuitFormUpgrade", non_name="Stealth")
    ]

