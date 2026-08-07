from . import *

# Clear the Road

def GetAbilities() -> Sequence['Ability']:

    def clear_the_road(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardDeckTopCards(10, effect)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.SchemeCannotLeavlPlayWhile(
            "This",
            while_card_is_in_play=CardFinder(name=BULLDOZER),
        ),
        IfThereIsTenThreatThenRemoveAllButThree(
            clear_the_road
        )
    ]

