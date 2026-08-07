from . import *

# Pile It On!

def GetAbilities() -> Sequence['Ability']:

    def pile_it_on(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardControlCards(effect, upgrade=True, support=True, highest_cost=True)

        Players.ForEachPlayer(effect, action)

    return [
        AbilityFactory.SchemeCannotLeavlPlayWhile(
            "This",
            while_card_is_in_play=CardFinder(name=PILEDRIVER),
        ),
        IfThereIsTenThreatThenRemoveAllButThree(pile_it_on)
    ]

