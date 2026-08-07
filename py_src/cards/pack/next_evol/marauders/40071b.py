from . import *

# * Blockbuster

def GetAbilities() -> Sequence['Ability']:

    def blockbuster(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        def action():
            message.GainAttackForThisAttack(+2, effect)
            message.GainOverKill(effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give Blockbuster a tough status card",
                lambda targets:
                    Faces.GiveStatus(targets, "Tough", effect)
            ).SetTarget([message.attacker]),
            AbilityFactory.ForChoiceAbility(
                "Blockbuster gets +2 ATK for this attack and this attack gains overkill",
                lambda targets:
                    action()
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

