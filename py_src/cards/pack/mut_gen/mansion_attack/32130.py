from . import *

# Save the School

def GetAbilities() -> Sequence['Ability']:

    def save_the_school(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        size = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=Villain))

        """
        Skirmish Mode: Defeat 1 villain to win.
        Standard Mode: Defeat 2 villains to win.
        Expert Mode: Defeat 3 villains to win.
        Heroic Mode: Defeat 4 villains to win.
        """

        target = 3 if Worlds.IsExpert(effect) else 2

        if size >= target:
            Worlds.SetGameOver(True, effect)

        else:
            def action(player: 'Player'):
                player.DealEncounterCards(1, effect)
            Players.ForEachPlayer(effect, action)

            scenario = Worlds.GetScenario(effect)
            villain = scenario.villain_deck.Get()[0]

            minion = Worlds.FindCardOnField(
                effect,
                name=villain.name,
                card_type=Minion
            )

            player = None

            if minion:
                player = minion.GetEngagedPlayer()
                Faces.DiscardAll([minion], effect)

            villain.PutIntoPlay("FirstPlayer", effect)

            if player:
                villain.DoActivate(player, effect)

    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            Villain,
            save_the_school
        ),
    ]

