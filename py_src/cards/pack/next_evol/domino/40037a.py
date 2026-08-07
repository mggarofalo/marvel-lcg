from . import *

# * Domino

def GetAbilities() -> Sequence['Ability']:

    def domino_counting(effect: 'Effect', message: 'Message.WhenCountingResourcesOnCards') -> None:
        face = message.face
        res = FacesCounter.GetPrintedResources([face])
        if res.g:
            return_value = res + \
                Resources.FromText("G" * res.g)
            message.SetReturnValue(return_value, effect)

    def domino(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)
    
        initiator = effect.GetInitiator()
        Faces.SwapWithDeckTop(effect.targets, initiator.player_deck, effect)

    return [
        # When counting resources on cards discarded from the top of the deck, count each printed [wild] icon twice.
        AbilityFactory.WhenCountingResourcesOnCards(
            None,
            domino_counting,
            from_where=lambda effect:
                effect.GetInitiator().player_deck,
            conditions=[
                lambda effect, message:
                    message.face.printed_resource_internal.g > 0
            ],
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            domino
        ).SetTarget(from_where=["YourHandCards"])
        .LimitOncePerRound(),
    ]

