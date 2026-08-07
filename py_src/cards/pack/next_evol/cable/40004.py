from . import *

# Precognition

def GetAbilities() -> Sequence['Ability']:

    def precognition(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        faces = initiator.LookAtDeck("EncounterDeck", value, effect)
        face = initiator.MayChooseFace(faces, effect, not_move=True)
        if face:
            faces.remove(face)
            Faces.DiscardAll([face], effect)
        initiator.PlaceOnTopInAnyOrder(faces, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            precognition
        ).SetPlay().SetLabel(),
    ]

