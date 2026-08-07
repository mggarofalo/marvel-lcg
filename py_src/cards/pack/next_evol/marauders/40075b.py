from . import *

# * Riptide

def GetAbilities() -> Sequence['Ability']:

    def riptide(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        def action(targets: Sequence['CardFace']):
            schemes = list(targets)
            for target in targets:
                if MainScheme.IsType(target):
                    this.PlaceThreatOnSchemes([target], 3, effect)
                    schemes.remove(target)
            this.PlaceThreatOnSchemes(schemes, 1, effect)

        def action_attack(targets: Sequence['CardFace']):
            message.GainAttackForThisAttack(+2, effect)
            message.GainPiercing(effect)
            message.GainRanged(effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 3 threat on the main scheme and 1 threat on each side scheme",
                action,
            ).SetTarget(Scheme2, can_place_threat=True, range="All"),
            AbilityFactory.ForChoiceAbility(
                "Riptide gets +2 ATK for this attack",
                action_attack
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

