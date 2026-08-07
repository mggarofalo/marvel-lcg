from . import *

# * Harpoon

def GetAbilities() -> Sequence['Ability']:

    def harpoon(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.target.GetControlByPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 2 indirect damage",
                lambda targets:
                    player.GetIdentity().TakeIndirectDamage(this, 2, effect)
            ),
            AbilityFactory.ForChoiceAbility(
                "Give Harpoon 1 additional facedown boost card for this attack",
                lambda targets:
                    Faces.GiveFacedownBoostCards(targets, 1, effect)
            ).SetTarget(name="Harpoon")
        )

    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            harpoon
        ),
    ]

