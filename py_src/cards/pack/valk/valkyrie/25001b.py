from . import *

# * Brunnhilde

def GetAbilities() -> Sequence['Ability']:

    def brunnhilde(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.SetAside(effect.targets, effect)


    return [
        AbilityFactory.SetupWithSetAside(
            ["25002"],
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            brunnhilde,
        ).SetName('"Not this Day."')
        .SetTarget(Upgrade, name=DEATHGLOW, from_where=["PlayersDiscardPile", "EncounterDiscardPile", "InPlay"])
    ]

