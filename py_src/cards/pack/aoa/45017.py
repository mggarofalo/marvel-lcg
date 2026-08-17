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

        faces = initiator.player_deck.Get() + initiator.discard_pile.Get()
        all_allies = Filter.ByType(faces, Ally)
        all_upgrades = Filter.ByType(faces, Upgrade)
        faces = all_allies + all_upgrades

        Faces.LookAt(faces, initiator, effect)

        def can_attach_to_ally(effect: 'Effect', upgrade: 'CardFace') -> bool:
            if Upgrade.IsType(upgrade):
                for ally in all_allies:
                    if upgrade.CanAttachTo(ally):
                        return True
                return False
            else:
                assert Ally.IsType(upgrade)
                return True

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    initiator.GainCard(targets, effect)
            ).SetTarget(faces,
                finder=CardFinder(
                    card_type=Upgrade|Ally,
                    check_effect_fn=can_attach_to_ally
                ),
                select_rule="DifferentType",
                range=(2, 2), by_search=True,
                check_again_fn=has_one_ally_and_one_attachable_upgrade
            ),
        )

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

    # Kept after MARVEL-127, which made `select_rule="DifferentType"` enforce
    # the chosen pair rather than only the pool. The one-of-each half of this
    # callback is now redundant -- with two targets over `Upgrade|Ally`, two
    # distinct types *is* one of each -- but the `CanAttachTo` half is not, and
    # no select rule can express it. So this is not the redundant callback the
    # issue expected to delete: removing it would drop a real restriction, and
    # the proof that `DifferentType` works lives in `test_select_rules.py`
    # against stand-ins instead.
    def has_one_ally_and_one_attachable_upgrade(effect: 'Effect', targets: Sequence['CardFace']) -> bool:
        allies = Filter.ByType(targets, Ally)
        upgrades = Filter.ByType(targets, Upgrade)
        if allies and upgrades:
            ally = allies[0]
            upgrade = upgrades[0]
            return upgrade.CanAttachTo(ally)
        return False

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            suit_up,
        ).SetPlay().SetLabel()
    ]

