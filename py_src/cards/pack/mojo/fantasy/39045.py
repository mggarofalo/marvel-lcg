from . import *

# Fetch Quest

def GetAbilities() -> Sequence['Ability']:

    def fetch_quest(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.PlayOneCardLikeInTurn(player.player_deck.Get(), effect, ignore_resources_cost=True, forced=True)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            fetch_quest,
        ),
    ]

