from . import *

# * Gwen Stacy

def GetAbilities() -> Sequence['Ability']:

    def gwen_stacy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Shuffle Ticket to the Multiverse from your discard pile into your deck",
                lambda targets:
                    Faces.ShuffleAllTo(targets, initiator.player_deck, effect)
            ).SetTarget(name="Ticket to the Multiverse", from_where=["YourDiscardPile"]),
            AbilityFactory.ForChoiceAbility(
                "Ready George Stacy",
                lambda targets:
                    Faces.ReadyAll(targets, effect),
            ).SetTarget(name="George Stacy", canbe_ready=True),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            gwen_stacy
        ).LimitOncePerRound()
        .SetTarget2(name="Ticket to the Multiverse", from_where=["YourDiscardPile"])
        .SetTarget2(name="George Stacy", canbe_ready=True),
    ]

