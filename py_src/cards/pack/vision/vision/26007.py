from . import *

# Density Control

def GetAbilities() -> Sequence['Ability']:

    def density_control(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            density_control,
            is_change_additional_form=True
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(Event, card_class="IdentitySpecific", from_where=["YourDiscardPile"]),
    ]

