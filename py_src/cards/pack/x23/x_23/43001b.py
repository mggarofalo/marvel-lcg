from . import *

# * Laura Kinney

def GetAbilities() -> Sequence['Ability']:

    def laura_kinney_shuffle(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        return Faces.ShuffleAllTo(targets, initiator.player_deck, effect) == targets

    def laura_kinney(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.SetupPutIntoPlay(
            ["43002"],
        ).SetName("Shhnk!"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            laura_kinney,
        ).SetCostFunc(CostFunc.Custom(
            Select.From(
                CardFinder(name="Honey Badger", card_type=Ally)|
                CardFinder(name="Sisterly Bond", card_type=Event),
                from_where=["YourDiscardPile"]
            ),
            laura_kinney_shuffle))
        .LimitOncePerRound(),
    ]

