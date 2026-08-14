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

    # The labels these two choices are offered under.
    #
    # Same rule as `SearchInternal.MAY_SEARCH_PROMPT` (MARVEL-112) and
    # `Players.DISCARD_ATTACHMENT_PROMPT` (MARVEL-116): an option built with an
    # empty name is rendered by `Effect.Render` as the *binding* effect's display
    # name, so it reads as the trigger that caused it rather than as anything
    # about itself. Both of these were reached only from encounter-card reveals
    # (56194 Cloak, 56195 Dagger), so both were offered as "When_Revealed".
    #
    # The amount is in the label because that is the house style for a choice
    # label throughout `cards/pack/` -- "Place 2 threat here", "Deal 3 damage to
    # an enemy" -- and because these two helpers are called with different
    # amounts by the same card, so a fixed string would render two different
    # decisions identically.
    PLACE_THREAT_PROMPT = "Place {value} threat on a scheme"
    DEAL_DAMAGE_PROMPT = "Deal {value} damage to a character you control"

    @staticmethod
    def PlaceThreatOnOneScheme(player: 'Player', value: int, by_effect: 'Effect'):
        player.ChooseAbilities(
            by_effect,
            AbilityFactory.ForChoiceAbility(
                Utility.PLACE_THREAT_PROMPT.format(value=value),
                lambda targets:
                    by_effect.this.PlaceThreatOnSchemes(targets, value, by_effect)
            ).SetTarget(Scheme2, can_place_threat=True)
        )

    @staticmethod
    def DealDamageToCharacterYouControl(player: 'Player', value: int, by_effect: 'Effect'):
        player.ChooseAbilities(
            by_effect,
            AbilityFactory.ForChoiceAbility(
                Utility.DEAL_DAMAGE_PROMPT.format(value=value),
                lambda targets:
                    by_effect.this.DealDamage(targets, value, by_effect)
            ).SetTarget(player.GetControlCharacters())
        )

