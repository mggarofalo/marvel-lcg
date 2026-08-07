from . import *

# * Vertigo

def GetAbilities() -> Sequence['Ability']:

    def vertigo(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Enemy)
        Unused(this)

        player = message.target.GetControlByPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Stun a character you control",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect)
            ).SetTarget("YouControlUnit", canbe_stunned=True),
            AbilityFactory.ForChoiceAbility(
                "Vertigo gets +2 ATK for this attack",
                lambda targets:
                    message.GainAttackForThisAttack(+2, effect)
            )
        )


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "This",
            CardFinder(card_type=Identity|Ally),
            vertigo
        ),
    ]

