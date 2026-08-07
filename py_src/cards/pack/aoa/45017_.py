from . import *

# Suit Up

def GetAbilities() -> Sequence['Ability']:

    # def can_be_attached_to(upgrade: 'Upgrade', ally: 'Ally') -> bool:
    #     play_effect = upgrade.FindEffect(sub_type="Play")
    #     if play_effect:
    #         # TODO: Fix
    #         # if play_effect.ability.select_fn not in [Select.OnFieldAllies, Select.OnFieldFriendlyCharacters, Select.OnFieldCharacters, Select.YourAllies]:
    #         #     return False

    #         filter_fn_list = play_effect.ability.filter_fn_list[:]
    #         select_target = Filter(lambda effect: [ally], filter_fn_list)
    #         return ally in select_target.GetFilteredTargets(GameRule(upgrade))
    #     return False
    #     return True

    def suit_up(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        # initiator.GainCard(effect.targets, effect)

        ally = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally,
            not_move=True,
        )

        if ally:
            upgrade = Search.PlayerCard(
                effect,
                initiator,
                include_player_deck=True,
                include_discard_pile=True,
                card_type=Upgrade,
                canbe_attach_to=ally,
            )

            if upgrade:
                initiator.GainCard([ally, upgrade], effect)

    # def check_has_ally_and_upgrade(effect: 'Effect') -> Sequence['Ally']:
    #     initiator = effect.GetInitiator()
    #     deck_cards = initiator.player_deck.Get(True) + initiator.discard_pile.Get(True)
    #     upgrades = [x for x in deck_cards if Upgrade.IsType(x)]
    #     if upgrades == []:
    #         return []
    #     def has_upgrade(ally: Ally):
    #         for upgrade in upgrades:
    #             if can_be_attached_to(upgrade, ally):
    #                 return True
    #         return False
    #     return [x for x in deck_cards if Ally.IsType(x) and has_upgrade(x)]

    # def has_one_ally_and_one_attachable_upgrade(effect: 'Effect', targets: List['CardFace']) -> bool:
    #     ally = Faces.FindCard(targets, card_type=Ally)
    #     upgrade = Faces.FindCard(targets, card_type=Upgrade)
    #     assert ally and upgrade

    #     play_abilities= upgrade.FindAbilities(sub_type="Play")
    #     for ability in play_abilities:
    #         if ability.selector and ability.selector.FilterLegalTargets([ally], effect):
    #             return True
    #     return False

    # def can_attach_to_ally(effect: 'Effect', upgrade: 'CardFace') -> bool:
    #     if Upgrade.IsType(upgrade):
    #         all_allies = Select.From("YourPlayerDeck",
    #             CardFinder(card_type=Ally),
    #             additional_where="YourDiscardPile",
    #         ).GetAllLegalTargets(effect)

    #         for ally in all_allies:
    #             if upgrade.CanAttachTo(ally):
    #                 return True
    #         return False
    #     else:
    #         assert Ally.IsType(upgrade)
    #         return True

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            suit_up,
        ).SetPlay().SetLabel()
        # .SetTarget2(Select.From("YourPlayerDeck",
        #     CardFinder(card_type=Upgrade),
        #     additional_where="YourDiscardPile",
        #     check_fn=can_attach_to_ally,

        #     additional_select=Select("YourPlayerDeck",
        #         CardFinder(card_type=Ally),
        #         additional_where="YourDiscardPile",
        #     ),

        #     select_rule="DifferentType",
        #     range=(2, 2), peek=True,
        #     check_again_fn=has_one_ally_and_one_attachable_upgrade
        # )),
    ]
    # Q: if there is not a fit upgrade for ally that I chose but I have a fit upgrade in my hand and I can use it to pay "Suit Up", can I use this card?

