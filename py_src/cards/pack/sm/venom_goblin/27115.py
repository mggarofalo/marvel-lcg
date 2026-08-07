from . import *

# * Venom Goblin (III)

def GetAbilities() -> Sequence['Ability']:

    def venom_goblin_iii(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        scheme = MoveGliderToTheMainSchemeWithLeastThreat(player, effect, False)

        this.PlaceThreatOnSchemes([scheme], 1, effect)
        scheme.ResolveSpecialAbility(player, effect)


    def deal_3_facedown_encounter_cards_to_each_player(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(3, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            deal_3_facedown_encounter_cards_to_each_player
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            venom_goblin_iii,
        ).SetName("Reign of Terror")
    ]

