from . import *

# * Miles Morales

def GetAbilities() -> Sequence['Ability']:

    def miles_morales(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            miles_morales,
            to_this_form=True,
        ).SetTarget(card_class="IdentitySpecific", from_where=["YourDiscardPile"]),
    ]

