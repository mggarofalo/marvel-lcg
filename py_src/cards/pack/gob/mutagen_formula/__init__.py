from cards.pack import *

def PlaceThreatOnMainSchemeAfterAttacksAndDamageYou(num: int) -> 'Ability':

    def place_threat(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this
        Unused(this)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], num, effect)

    return AbilityFactory.AfterUnitAttackAndDamageUnit(
        AbilityType.ForcedResponse,
        "This",
        "You",
        place_threat,
    )

def DealEncounterCardsToEachPlayer(num: int) ->'Ability':
    def deal_encounter_cards_to_each_player(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this
        Unused(this)

        def action(player: 'Player'):
            player.DealEncounterCards(num, effect)

        Players.ForEachPlayer(effect, action)

    return AbilityFactory.WhenThisRevealed(
        None,
        deal_encounter_cards_to_each_player
    )


