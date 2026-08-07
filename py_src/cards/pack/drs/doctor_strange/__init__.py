from cards.pack import *

def SelectInvocationTopCard(effect: 'Effect') -> Sequence['CardFace']:
    initiator = effect.GetInitiator()
    face = initiator.additional_deck.GetTop()
    if face:
        return [face]
    else:
        return []

def InvocationTopCardHasTarget(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> bool:
    initiator = effect.GetInitiator()
    face = initiator.additional_deck.GetTop()
    if face:
        face = face.CastTo(Event)
        for effect in face.effects:
            if effect.ability.when == Message.WhenResolveSpecialAbility and \
                effect.checker.UpdateLegalTargets():
                return True
    return False

def GetCostOfInvocationTopCard(effect: 'Effect', targets: List['CardFace']) -> Cost:
    initiator = effect.GetInitiator()

    face = initiator.additional_deck.GetTop()
    if face:
        face = face.CastTo(Event)
        return face.printed_cost
    return Cost("0")

