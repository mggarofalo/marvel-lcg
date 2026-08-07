from . import *

# View the Cosmos

def GetAbilities() -> Sequence['Ability']:

    def view_the_cosmos_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            scheme = Worlds.FindMainScheme(this)
            if scheme:
                value = FacesCounter.GetPrintedCost(targets)
                this.PlaceThreatOnSchemes([scheme], value, effect)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Put the highest cost card from your hand faceup into The Collection",
                lambda targets:
                    PutCardIntoTheCollection(targets, effect)
            ).SetTarget(highest_cost=True, from_where=["YourHandCards"]),
            AbilityFactory.ForChoiceAbility(
                "Discard the highest cost card from your hand, then place threat on the main scheme equal to its printed cost",
                action,
            ).SetTarget(highest_cost=True, from_where=["YourHandCards"], canbe_discard=True)
            # .SetTarget2(MainScheme, can_place_threat=True),
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            view_the_cosmos_revealed
        ),
    ]

