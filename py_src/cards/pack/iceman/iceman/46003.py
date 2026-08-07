from . import *

# Snow Clone

def GetAbilities() -> Sequence['Ability']:

    def snow_clone(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.GainForThisActive(
            effect,
            message.would_atk_message,
            attack_consequential_damage=-1,
        )


    return [
        *AbilityFactory.UnitCannotHaveUpgradeAttached(
            "This"
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.NonKeyword,
            "This",
            CardFinder(with_attach=CardFinder(name="Frostbite")),
            snow_clone
        ),
    ]

