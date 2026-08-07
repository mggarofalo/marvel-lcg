from . import *

# Ritual Combat

def GetAbilities() -> Sequence['Ability']:

    def ritual_combat_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        player = message.GetToPlayer()

        faces = Worlds.DiscardEncounterCards(1, effect)
        value = FacesCounter.CountTotalBoostIcons(faces) + 1

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                f"deal {value} damage to your hero",
                lambda targets:
                    this.DealDamage(targets, value, effect)
            ).SetTarget("YourHero"),
            AbilityFactory.ForChoiceAbility(
                f"place {value} threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, value, effect)
            ).SetTarget(MainScheme, can_place_threat=True),
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ritual_combat_revealed
        )
    ]

