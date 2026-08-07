from . import *

# Telepathic Camouflage

def GetAbilities() -> Sequence['Ability']:

    def telepathic_camouflage_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            value = FacesCounter.GetMostCommonType(player.hand_cards.Get())
            player.GetIdentity().PlaceThreatOnSchemes([this], value, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            telepathic_camouflage_revealed
        ),
    ]

