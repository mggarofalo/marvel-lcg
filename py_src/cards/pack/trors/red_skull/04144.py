from . import *

# Mass Chaos

def GetAbilities() -> Sequence['Ability']:

    def mass_chaos_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.DiscardDeckTopCards(5, effect)
            res = FacesCounter.GetPrintedResources(faces)
            this.PlaceThreatOnSchemes([this], res.GetResourceIconTypes(), effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mass_chaos_revealed
        ),
    ]

