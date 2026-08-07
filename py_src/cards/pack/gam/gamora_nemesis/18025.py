from . import *

# Sibling Rivalry

def GetAbilities() -> Sequence['Ability']:

    def sibling_rivalry(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.FindPlayerByName("Gamora", effect)
        if player:
            player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.PlayersCannotRemoveThreatFrom(
            PlayerFinder(non_name="Gamora"),
            "This"
        ),
        AbilityFactory.AfterPhaseBegin(
            AbilityType.WhenDefeated,
            "Villain",
            sibling_rivalry,
        ),
    ]

