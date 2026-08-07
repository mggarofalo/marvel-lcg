from . import *

# * Blockbuster

def GetAbilities() -> Sequence['Ability']:

    def blockbuster(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give Blockbuster a tough status card",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget([message.attacker]),
            AbilityFactory.ForChoiceAbility(
                "Blockbuster gets +2 ATK for this attack",
                lambda targets:
                    message.GainAttackForThisAttack(+2, effect)
            )
        )

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            blockbuster
        ),
    ]

