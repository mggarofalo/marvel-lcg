from . import *

# Power Drain

def GetAbilities() -> Sequence['Ability']:

    def power_drain(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.DiscardEncounterCards(2, effect)
        size = FacesCounter.CountTotalBoostIcons(faces)

        def action(player: 'Player'):
            Players.DiscardResourceIconFromHand(player, size, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            power_drain
        ),
    ]

