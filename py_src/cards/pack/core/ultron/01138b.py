from . import *

# Assault on NORAD - 2B

def GetAbilities() -> Sequence['Ability']:

    def assault_on_norad(effect: 'Effect', message: 'Message.WhenMainSchemePhaseEnd') -> None:
        this = effect.this.CastTo(MainScheme)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Place 2 threat here",
                    lambda targets:
                        this.PlaceThreatOnSchemes(targets, 2, effect)
                ).SetTarget("This"),
                AbilityFactory.ForChoiceAbility(
                    "Put the top card of their deck into play facedown, engaged with them as a DRONE minion",
                    lambda targets:
                        CreateUltronFacedownDrone(player, 1, effect)
                )
            )

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.WhenMainSchemePhaseEnd(
            AbilityType.ForcedResponse,
            assault_on_norad
        )
    ]
