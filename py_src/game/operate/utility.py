from . import *

@final
class Utility:
    @staticmethod
    def DealEachPlayerEncounterCard(by_effect: 'Effect'):
        def action(player: 'Player'):
            player.DealEncounterCards(1, by_effect)
        Players.ForEachPlayer(by_effect, action)

    @staticmethod
    def CannotTakeDamageThisPhase(face: 'CardFace', by_effect: 'Effect'):
        face.effect.RegisterTemp(
            AbilityFactory.ThisCannotTakeDamageWhile(
                conditions=True
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )

    @staticmethod
    def PlaceThreatOnOneScheme(player: 'Player', value: int, by_effect: 'Effect'):
        player.ChooseAbilities(
            by_effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    by_effect.this.PlaceThreatOnSchemes(targets, value, by_effect)
            ).SetTarget(Scheme2, can_place_threat=True)
        )

    @staticmethod
    def DealDamageToCharacterYouControl(player: 'Player', value: int, by_effect: 'Effect'):
        player.ChooseAbilities(
            by_effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    by_effect.this.DealDamage(targets, value, by_effect)
            ).SetTarget(player.GetControlCharacters())
        )

