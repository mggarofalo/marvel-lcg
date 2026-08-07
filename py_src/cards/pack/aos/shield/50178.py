from . import *

# S.H.I.E.L.D. Trooper

def GetAbilities() -> Sequence['Ability']:

    def shield_trooper(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discards 1 [[S.H.I.E.L.D.]] ally or support",
                action
            ).SetTarget("YouControlCards", trait="S.H.I.E.L.D", card_type="Ally|Support", canbe_discard=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 2, effect)
            ).SetTarget(MainScheme, can_place_threat=True)
        )


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            shield_trooper
        ),
    ]

