from . import *

# * Harpoon

def GetAbilities() -> Sequence['Ability']:

    def harpoon(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.target.GetControlByPlayer()

        def action(targets: Sequence['CardFace']):
            message.GainAttackForThisAttack(+2, effect)
            message.GainPiercing(effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 2 indirect damage",
                lambda targets:
                    player.GetIdentity().TakeIndirectDamage(this, 2, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "Harpoon gets +2 ATK for this attack and this attack gains piercing",
                action
            )
        )

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            harpoon
        ),
    ]

