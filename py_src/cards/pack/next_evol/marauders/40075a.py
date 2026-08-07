from . import *

# * Riptide

def GetAbilities() -> Sequence['Ability']:

    def riptide(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        def action(targets: Sequence['CardFace']):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
            this.PlaceThreatOnSchemes("EachSideScheme", 1, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 2 threat on the main scheme and 1 threat on each side scheme",
                action,
            ).SetTarget(Scheme2, can_place_threat=True, range="All"),
            AbilityFactory.ForChoiceAbility(
                "Riptide gets +2 ATK for this attack",
                lambda targets:
                    message.GainAttackForThisAttack(+2, effect)
            ).SetTarget("This")
        )


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            riptide
        ),
    ]

