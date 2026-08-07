from . import *

# Time-Travel Hijinks

def GetAbilities() -> Sequence['Ability']:

    def time_travel_hijinks(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        player = message.GetToPlayer()
        face = player.DiscardControlCards(effect, control=True, highest_cost=True)
        if face:
            this.PlaceCardHere(face, False, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            time_travel_hijinks
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            has_printed_res="Y",
        ))
    ]

