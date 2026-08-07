from . import *

# Badoon Blitz

def GetAbilities() -> Sequence['Ability']:

    def badoon_blitz(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Choose and discard 1 card from their hand",
                    lambda targets:
                        Faces.DiscardAll(targets, effect)
                ).SetTarget("YourHandCards", canbe_discard=True)
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.ExpertModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            badoon_blitz,
        ),
    ]

