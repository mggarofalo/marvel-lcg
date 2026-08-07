from . import *

# War Delivery

def GetAbilities() -> Sequence['Ability']:

    def war_delivery_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if not player.AskSpendResources(Cost("G"), effect):
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.DoAttackYou(player, effect)
            warbringer = Worlds.FindCardOnField(
                effect,
                name="Warbringer",
                card_type=Minion
            )
            if warbringer:
                warbringer.DoAttackYou(player, effect)

    def war_delivery_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        value = player.hand_cards.FindCardSize(CardFinder(has_printed_res="G"))
        
        this.PlaceThreatOnSchemes("MainScheme", value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            war_delivery_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            war_delivery_boost
        ),
    ]

