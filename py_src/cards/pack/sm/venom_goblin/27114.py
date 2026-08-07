from . import *

# * Venom Goblin (II)

def GetAbilities() -> Sequence['Ability']:

    def venom_goblin_ii(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetAgainstPlayer()
        scheme = MoveGliderToTheMainSchemeWithLeastThreat(player, effect, False)

        scheme.ResolveSpecialAbility(player, effect)

    def deal_2_facedown_encounter_cards_to_each_player(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(2, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            deal_2_facedown_encounter_cards_to_each_player
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            venom_goblin_ii,
        ).SetName("Claim the Throne")
    ]

