from . import *

# Under Attack

def GetAbilities() -> Sequence['Ability']:

    def under_attack(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        def place_2_threat(targets: Sequence['CardFace']) -> None:
            this.PlaceThreatOnSchemes(targets, 2, effect)

        def deal_3_damage(targets: Sequence['CardFace']) -> None:
            this.DealDamage(targets, 3, effect)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Place 2 threat here",
                    place_2_threat
                ).SetTarget("This"),
                AbilityFactory.ForChoiceAbility(
                    "Deal 3 damage to their hero",
                    deal_3_damage
                ).SetTarget("YourHero")
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            under_attack
        ),
    ]
