from . import *

# * Blade: Eric Brooks

def GetAbilities() -> Sequence['Ability']:

    def blade(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("R", from_hand=True)
            ),
            AbilityFactory.ForChoiceAbility(
                "Discard Blade",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget([this], canbe_discard=True)
        )


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            blade
        ),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.ForcedResponse,
            "This",
            blade
        )
    ]

