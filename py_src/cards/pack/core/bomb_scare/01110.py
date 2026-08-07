from . import *

# Hydra Bomber

def GetAbilities() -> Sequence['Ability']:

    def hydra_bomber(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        player = message.GetToPlayer()

        def take_2_damage(targets: Sequence['CardFace']) -> None:
            for target in targets:
                if Unit2.IsType(target):
                    target.TakeDamage(this, 2, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 2 damage",
                take_2_damage
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Place 1 threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 1, effect)
            ).SetTarget(MainScheme, can_place_threat=True)
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hydra_bomber
        )
    ]

