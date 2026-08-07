from . import *

# Scrying

def GetAbilities() -> Sequence['Ability']:

    def scrying(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck(initiator.player_deck, 3, effect)

        # you can play Scrying even if you have less than 3 cards left in your deck, and you can choose what to do with the remaining cards.
        left = len(faces)
        face = initiator.AskChooseFace(faces, effect, forced = left==3, prompt="Draw one")
        if face:
            left -= 1
            initiator.GainCard(face, effect)
            faces.remove(face)

        face = initiator.AskChooseFace(faces, effect, forced = left==2, prompt="Discard one")
        if face:
            left -= 1
            Faces.DiscardAll([face], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            scrying
        ).SetPlay().SetLabel(),
    ]

