from . import *

# Badoon Blitz

def GetAbilities() -> Sequence['Ability']:

    def badoon_blitz(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.MayChooseOneAbility(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Draw 1 card",
                    lambda targets:
                        player.DrawUp(1, effect)
                )
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.StandardModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            badoon_blitz
        ),
    ]

