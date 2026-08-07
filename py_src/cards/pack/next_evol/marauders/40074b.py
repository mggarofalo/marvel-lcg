from . import *

# * Harpoon

def GetAbilities() -> Sequence['Ability']:

    def harpoon(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.target.GetControlByPlayer()

        def action():
            message.GiveAdditionalBoostCardForThisActivation(1, effect)
            message.GainOverKill(effect)


        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 3 indirect damage",
                lambda targets:
                    player.GetIdentity().TakeIndirectDamage(this, 3, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "Give Harpoon 1 additional facedown boost card for this attack. This attack gains overkill",
                lambda targets:
                    action()
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

