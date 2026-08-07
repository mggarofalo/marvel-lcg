from . import *

# Global Logistics

def GetAbilities() -> Sequence['Ability']:

    def global_logistics(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        deck = effect.targets[0].card.area
        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck(deck, 4, effect)
        discards = initiator.AskDiscardFaces(faces, "Any", effect)
        rest = Faces.GetRest(faces, discards)
        initiator.PlaceOnTopAndOrBottomInAnyOrder(rest, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            global_logistics
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(trait="S.H.I.E.L.D", from_where=["YouControlCards"]))
        .SetTarget(from_where=["PlayersDeckTop", "EncounterDeckTop"])
    ]

