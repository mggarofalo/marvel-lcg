from . import *

# * Spiral

def GetAbilities() -> Sequence['Ability']:

    def spiral_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            this.DoAttackYou(player, effect)
        Players.ForEachPlayer(effect, action)

    def spiral(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.RemoveCountersOn([this], "All", 'teleport', effect)
        this.card.Flip(effect)

    def spiral_place(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'teleport', effect)

    return [
        AbilityFactory.IfThereAreAtLeastCounterHere(
            "3*",
            'teleport',
            spiral
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            spiral_revealed
        ),
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            spiral_place,
        ),
    ]

