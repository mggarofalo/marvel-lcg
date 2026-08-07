from . import *

# * The Painted Lady

def GetAbilities() -> Sequence['Ability']:

    def the_painted_lady(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.PlaceCardHere(message.trigger, False, effect)

    def the_painted_lady_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)
        
        initiator = effect.GetInitiator()
        Faces.AddToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.AfterCardDiscardInternal(
            AbilityType.Response,
            None,
            the_painted_lady,
            # from_top_of_player_deck=True,
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().GetSize() < 3 and \
                    effect.GetInitiator().player_deck == message.from_area
            ],
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            the_painted_lady_action
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(from_where=["ThisPlaceCards"])
    ]

