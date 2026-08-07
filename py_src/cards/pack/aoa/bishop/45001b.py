from . import *

# * Lucas Bishop

def GetAbilities() -> Sequence['Ability']:

    def lucas_bishop(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "This",
            lucas_bishop,
            to_this_form=True,
        ).SetTarget(trait="TEMPORAL", from_where=["YourDiscardPile"])
        .SetName("Temporally Displaced"),
    ]

