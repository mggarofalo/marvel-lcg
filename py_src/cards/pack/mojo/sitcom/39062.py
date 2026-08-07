from . import *

# Growing Pains

def GetAbilities() -> Sequence['Ability']:

    def growing_pains(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)

        initiator = effect.GetInitiator()
        if initiator.discard_pile.FindCardSize(CardFinder(card_type=Upgrade)) > len(initiator.GetControlUpgrade()):
            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.EachCardYouPlayCostAdditionalResource(
            Upgrade,
            +2
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            growing_pains,
        ).SetTarget(Upgrade, from_where=["YouControlCards", "YourHandCards"])
    ]

