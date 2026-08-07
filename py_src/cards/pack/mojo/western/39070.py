from . import *

# A Game of Cards

def GetAbilities() -> Sequence['Ability']:

    def a_game_of_cards_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.DrawUp(5, effect)
        faces = player.DiscardHandCards(("Zero", 5), effect)
        num = FacesCounter.GetPrintedResourcesTypes(faces)
        player.DiscardControlCards(effect, upgrade=True, support=True, range=("Zero", num))
        # if num >= len(control_faces):
        #     player.AskDiscardSome2(control_faces, "All", effect)
        # else:
        #     player.AskDiscardSome2(control_faces, (num, num), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            a_game_of_cards_revealed
        ),
    ]

