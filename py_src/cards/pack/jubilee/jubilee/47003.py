from . import *

# * Shopping Spree

def GetAbilities() -> Sequence['Ability']:

    def shopping_spree(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)
        
        this.RemoveThreatFromSchemes([this], 1, effect)

    def shopping_spree_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_discard_pile=True,
            trait="ITEM",
        )
        if face:
            face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            "This",
            by_character=CardFinder(card_type=Ally|Hero)
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            shopping_spree
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
        .AnyPlayerCanDoThis(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            shopping_spree_defeated,
            has_defeating_player=True
        ),
    ]

