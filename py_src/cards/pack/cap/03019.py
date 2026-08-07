from . import *

# Quinjet

def GetAbilities() -> Sequence['Ability']:

    def quinjet_response(effect: 'Effect', message: 'Message.AfterPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Support)
        Faces.PlaceCountersOn([this], 1, 'time', effect)

    def quinjet(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        effect.targets[0].PutIntoPlay(initiator, effect)
        Faces.DiscardAll([this], effect)

    def check_time_counter(effect: 'Effect', face: 'CardFace') -> bool:
        counter = effect.this.CastTo(Support).GetCounters('time')
        return FacesCounter.GetPrintedCost([face]) <= counter

    return [
        AbilityFactory.AfterPlayerTurnBegin(
            AbilityType.Response,
            "This",
            quinjet_response
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            quinjet
        ).SetTarget(Ally, trait="AVENGER", check_fn=check_time_counter, from_where=["YourHandCards"])
    ]

