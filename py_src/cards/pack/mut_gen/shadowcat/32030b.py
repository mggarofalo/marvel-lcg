from . import *

# * Kitty Pride

def GetAbilities() -> Sequence['Ability']:

    def kitty_pride(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.form.ChangeMassForm(effect)


    return [
        AbilityFactory.SetupPutIntoPlay(
            ["32031a,32031b"],
            flip_to_name="Solid"
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            kitty_pride
        ).LimitOncePerRound(),
    ]

