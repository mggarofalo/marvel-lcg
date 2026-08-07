from . import *

# * Arclight

def GetAbilities() -> Sequence['Ability']:

    def arclight(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Confuse the character you control with the highest THW",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget("YouControlUnit", canbe_confused=True, highest_thw=True),
            AbilityFactory.ForChoiceAbility(
                "Arclight gets +2 ATK for this attack",
                lambda targets:
                    message.GainAttackForThisAttack(+2, effect)
            )
        )

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            arclight
        ),
    ]

