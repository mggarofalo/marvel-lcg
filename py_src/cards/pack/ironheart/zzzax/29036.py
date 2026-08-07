from . import *

# Feedback Loop

def GetAbilities() -> Sequence['Ability']:

    def feedback_loop_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.hand_cards.GetAll() + player.GetControlCards()
            num = FacesCounter.GetPrintedResourcesCount(faces, "Y")
            this.PlaceThreatOnSchemes([this], num, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            feedback_loop_revealed
        ),
    ]

