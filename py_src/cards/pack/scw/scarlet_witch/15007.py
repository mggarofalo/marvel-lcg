from . import *

# * Agatha Harkness

def GetAbilities() -> Sequence['Ability']:

    def agatha_harkness(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.LookAtDeck(initiator.player_deck, 3, effect)
        face = initiator.AskChooseFace(faces, effect)
        if face:
            faces.remove(face)
            initiator.GainCard(face, effect)

        initiator.PlaceOnBottomInAnyOrder(faces, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            agatha_harkness
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

