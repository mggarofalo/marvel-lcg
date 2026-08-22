"""The descriptor a client receives must contain no card it may not see.

`ToDescriptor.World` builds one `WorldDescriptor` per render for the whole
table, and `GameServerSync.handle_get_world` used to hand all of it to whichever
client asked -- every card's `card_id` and `name`, in deck order, with
`visible_for_players` alongside as an instruction the *browser* was trusted to
follow. So the encounter deck's order, every other player's hand and the
identity of every face-down card in play were readable with a `curl` and a valid
`app_version` cookie. That was MARVEL-62.

`engine/device/web/server/world_visibility.py` moves the decision to the server.
These tests are what says it stayed there.

Three of them are structural rather than behavioural, and they are the ones that
matter in a year:

- `TestEveryZoneIsFiltered` fills **every** card-carrying field on
  `WorldDescriptor` off its own annotations and requires all of them to come
  back redacted, so a zone added later is covered the day it is added.
- `TestEveryCardFieldIsAccountedFor` requires every field of `CardDescriptor` to
  be named in exactly one of two lists -- blanked, or deliberately allowed
  through. A new field fails the test rather than leaking quietly.
- `TestTheSharedDescriptorIsNotMutated` pins that filtering copies. The
  descriptor is built once and read by every client; redacting it in place would
  blank the board for the player whose card it is.

No engine boot: these build descriptors directly, so they belong in the fast
tier. They use the real `CardDescriptor` and `WorldDescriptor` classes, though,
because the point of the structural tests is to notice when those change.
"""

import unittest
from dataclasses import fields
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.web.server import world_visibility
from engine.device.web.server.server_sync import GameServerSync
from engine.device.web.server.world_visibility import (
    BlankFace, RedactForViewers, ResolveViewers)
from game.render.descriptor.card import CardDescriptor
from game.render.descriptor.world import WorldDescriptor


def NewCard(object_id, *, visible_for, name=None, card_id=None):
    """A card that looks like something `Card.Render` would have produced."""
    return CardDescriptor(
        id                  = object_id,
        is_ready            = True,
        is_face_up          = False,
        visible_for_players = list(visible_for),
        bind_object_id      = 0,
        effect_by_cards     = [7],
        effect_to_cards     = [8],
        game_area           = 1,
        name                = name or f"Card {object_id}",
        info                = {"health": 3},
        traits              = {"Aerial": 1},
        buffs               = {"attack": "+1"},
        card_id             = card_id or f"0110{object_id}",
        pic_id              = f"0110{object_id}",
        down_card_ids       = ["encounter"],
        effects             = [11],
        resources           = [12],
        card_type           = "Treachery",
        cost                = 2,
        revision            = 2922126080,
        is_new              = True,
        is_action           = True,
    )


def AllCards(node):
    """Every `CardDescriptor` anywhere under `node`."""
    if world_visibility.IsCardDescriptor(node):
        yield node
        return
    if hasattr(node, '__dataclass_fields__'):
        for descriptor_field in fields(node):
            yield from AllCards(getattr(node, descriptor_field.name))
        return
    if isinstance(node, (list, tuple)):
        for item in node:
            yield from AllCards(item)


def IsRedacted(card):
    return all(getattr(card, name) == blank for name, blank in BlankFace().items())


class TestOneCard(unittest.TestCase):

    def test_a_card_the_viewer_may_see_is_untouched(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[0])])

        seen = RedactForViewers(world, [0]).encounter_deck[0]

        self.assertEqual(seen.card_id, "01101")
        self.assertEqual(seen.name, "Card 1")
        self.assertEqual(seen.visible_for_players, [0])

    def test_a_card_the_viewer_may_not_see_loses_its_face(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[1])])

        seen = RedactForViewers(world, [0]).encounter_deck[0]

        self.assertEqual(seen.card_id, "")
        self.assertEqual(seen.name, "")
        self.assertEqual(seen.pic_id, "")
        self.assertEqual(seen.card_type, "")
        self.assertEqual(seen.info, {})
        self.assertEqual(seen.traits, {})
        self.assertEqual(seen.buffs, {})
        self.assertEqual(seen.cost, 0)
        self.assertFalse(seen.is_new)
        self.assertFalse(seen.is_action)

    def test_the_revision_hash_goes_too(self):
        # It is a hash over the card's render info. Leaving it would let a
        # client watch a hidden card change, and fingerprint what it is.
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[1])])

        self.assertEqual(RedactForViewers(world, [0]).encounter_deck[0].revision, 0)

    def test_the_card_still_has_a_back_to_draw(self):
        # A redacted card is still rendered -- as the back it is physically
        # showing. Blank that and the browser draws nothing at all.
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[1])])

        seen = RedactForViewers(world, [0]).encounter_deck[0]

        self.assertEqual(seen.id, 1)
        self.assertEqual(seen.down_card_ids, ["encounter"])
        self.assertEqual(seen.game_area, 1)
        self.assertTrue(seen.is_ready)

    def test_the_visibility_list_is_emptied_rather_than_forwarded(self):
        # `visible_for_players` was the instruction the browser was trusted to
        # follow. Forwarding it would also say which player peeked at the card.
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[1, 2])])

        self.assertEqual(RedactForViewers(world, [0]).encounter_deck[0].visible_for_players, [])

    def test_a_card_visible_to_nobody_is_visible_to_nobody(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[])])

        self.assertEqual(RedactForViewers(world, [0]).encounter_deck[0].card_id, "")


class TestWhoIsAsking(unittest.TestCase):

    def test_any_of_the_asked_for_players_is_enough(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[2])])

        self.assertEqual(RedactForViewers(world, [0, 1, 2]).encounter_deck[0].card_id, "01101")

    def test_a_player_does_not_see_another_players_card(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[2])])

        self.assertEqual(RedactForViewers(world, [0, 1]).encounter_deck[0].card_id, "")

    def test_an_eliminated_viewer_sees_the_whole_table(self):
        # The browser already reveals the board to an eliminated player
        # (`descriptor.ts`, `isVisible`). Filtering must not take that away.
        world = WorldDescriptor(
            players=[
                WorldDescriptor.PlayerDescriptor(is_eliminated=True),
                WorldDescriptor.PlayerDescriptor(hand_cards=[NewCard(1, visible_for=[1])]),
            ])

        self.assertEqual(ResolveViewers(world, [0]), {0, 1})
        self.assertEqual(RedactForViewers(world, [0]).players[1].hand_cards[0].card_id, "01101")

    def test_a_live_viewer_does_not(self):
        world = WorldDescriptor(
            players=[
                WorldDescriptor.PlayerDescriptor(),
                WorldDescriptor.PlayerDescriptor(hand_cards=[NewCard(1, visible_for=[1])]),
            ])

        self.assertEqual(ResolveViewers(world, [0]), {0})
        self.assertEqual(RedactForViewers(world, [0]).players[1].hand_cards[0].card_id, "")

    def test_a_player_id_off_the_end_of_the_table_is_not_an_error(self):
        world = WorldDescriptor(encounter_deck=[NewCard(1, visible_for=[0])])

        self.assertEqual(RedactForViewers(world, [9]).encounter_deck[0].card_id, "")


class TestDeckOrder(unittest.TestCase):
    """Order is half the leak: object ids are stable for the whole game, so a
    card seen face-up once and shuffled back in would be trackable through a
    deck that came back in its real order."""

    def test_hidden_cards_come_back_in_object_id_order(self):
        deck = [NewCard(object_id, visible_for=[1]) for object_id in (5, 2, 9, 1)]
        world = WorldDescriptor(encounter_deck=deck)

        seen = RedactForViewers(world, [0]).encounter_deck

        self.assertEqual([card.id for card in seen], [1, 2, 5, 9])

    def test_the_deck_keeps_its_height(self):
        deck = [NewCard(object_id, visible_for=[1]) for object_id in (5, 2, 9, 1)]
        world = WorldDescriptor(encounter_deck=deck)

        self.assertEqual(len(RedactForViewers(world, [0]).encounter_deck), 4)

    def test_a_revealed_card_keeps_its_position(self):
        # "Look at the top card of the encounter deck" leaves one card visible
        # in an otherwise hidden zone. Reordering around it would misdraw it.
        deck = [
            NewCard(5, visible_for=[0]),
            NewCard(2, visible_for=[1]),
            NewCard(9, visible_for=[1]),
        ]
        world = WorldDescriptor(encounter_deck=deck)

        seen = RedactForViewers(world, [0]).encounter_deck

        self.assertEqual([card.id for card in seen], [5, 2, 9])
        self.assertEqual(seen[0].card_id, "01105")
        self.assertEqual(seen[1].card_id, "")

    def test_a_fully_visible_zone_is_left_exactly_as_it_was(self):
        deck = [NewCard(object_id, visible_for=[0]) for object_id in (5, 2, 9)]
        world = WorldDescriptor(encounter_discard_pile=deck)

        seen = RedactForViewers(world, [0]).encounter_discard_pile

        self.assertEqual([card.id for card in seen], [5, 2, 9])


class TestTheSharedDescriptorIsNotMutated(unittest.TestCase):

    def test_filtering_copies(self):
        card = NewCard(1, visible_for=[1])
        world = WorldDescriptor(encounter_deck=[card])

        RedactForViewers(world, [0])

        self.assertEqual(card.card_id, "01101")
        self.assertEqual(world.encounter_deck[0].card_id, "01101")

    def test_two_clients_get_their_own_answer(self):
        world = WorldDescriptor(
            players=[
                WorldDescriptor.PlayerDescriptor(hand_cards=[NewCard(1, visible_for=[0])]),
                WorldDescriptor.PlayerDescriptor(hand_cards=[NewCard(2, visible_for=[1])]),
            ])

        for player_id in (0, 1):
            seen = RedactForViewers(world, [player_id])
            mine = seen.players[player_id].hand_cards[0]
            theirs = seen.players[1 - player_id].hand_cards[0]
            self.assertNotEqual(mine.card_id, "")
            self.assertEqual(theirs.card_id, "")


class TestEveryZoneIsFiltered(unittest.TestCase):
    """Fill every card-carrying field the descriptor declares, hide all of it,
    and require all of it back redacted.

    Driven off the annotations rather than a written-down list of zones, so a
    zone added to `WorldDescriptor` or `PlayerDescriptor` later is covered
    without anyone remembering this file exists.
    """

    @staticmethod
    def FillWithHiddenCards(cls, counter):
        values = {}
        for descriptor_field in fields(cls):
            annotation = str(descriptor_field.type)
            if 'PlayerDescriptor' in annotation:
                values[descriptor_field.name] = [
                    TestEveryZoneIsFiltered.FillWithHiddenCards(
                        WorldDescriptor.PlayerDescriptor, counter)]
            elif 'Sequence[' in annotation and 'CardDescriptor' in annotation:
                counter.append(1)
                values[descriptor_field.name] = [[NewCard(len(counter), visible_for=[3])]]
            elif 'CardDescriptor' in annotation:
                counter.append(1)
                values[descriptor_field.name] = [NewCard(len(counter), visible_for=[3])]
        return cls(**values)

    def test_the_walk_reaches_every_card_carrying_field(self):
        counter = []
        world = self.FillWithHiddenCards(WorldDescriptor, counter)

        placed = list(AllCards(world))
        self.assertEqual(len(placed), len(counter))
        # Sanity: the descriptor really does have a lot of zones, so a walk that
        # silently found none would not pass by accident.
        self.assertGreater(len(placed), 20)

        seen = list(AllCards(RedactForViewers(world, [0])))
        self.assertEqual(len(seen), len(placed))
        for card in seen:
            self.assertTrue(IsRedacted(card), f"card {card.id} was not redacted")

    def test_and_leaves_them_alone_when_the_viewer_may_look(self):
        counter = []
        world = self.FillWithHiddenCards(WorldDescriptor, counter)

        for card in AllCards(RedactForViewers(world, [3])):
            self.assertFalse(IsRedacted(card), f"card {card.id} was redacted for a viewer who may see it")


class TestEveryCardFieldIsAccountedFor(unittest.TestCase):
    """Every field of `CardDescriptor` is either blanked or deliberately let
    through. A field added later is in neither list, and this fails."""

    # What a player can see across the table without reading the card: where it
    # sits, whether it is exhausted, what it is attached to, which cards point
    # at it, and the back that is facing up.
    ALLOWED_THROUGH = {
        'id', 'is_ready', 'is_face_up', 'bind_object_id', 'game_area',
        'down_card_ids', 'effect_by_cards', 'effect_to_cards',
    }

    def test_no_field_is_unclassified(self):
        declared = {descriptor_field.name for descriptor_field in fields(CardDescriptor)}
        classified = set(BlankFace()) | self.ALLOWED_THROUGH

        self.assertEqual(declared - classified, set(),
            "a new CardDescriptor field is neither blanked nor allowed through -- "
            "decide which, in world_visibility.BlankFace or in this test")
        self.assertEqual(classified - declared, set(),
            "world_visibility blanks or allows a field CardDescriptor no longer has")

    def test_a_hidden_card_is_face_down_by_construction(self):
        # `Card.IsVisible` returns True for anything face up, so `is_face_up`
        # being allowed through leaks nothing: a redacted card is always False.
        self.assertIn('is_face_up', self.ALLOWED_THROUGH)


class TestWhichPlayersTheRequestAsksFor(unittest.TestCase):
    """`GameServerSync.get_view_player_ids` -- deliberately separate from
    `get_player_ids`, which also decides who a `/client_updated` acknowledges
    for and who a `/post` speaks for."""

    @staticmethod
    def Ask(query, table_size=3, player_ids=(2,)):
        server = mock.Mock()
        server.controller_manager.total_players = table_size
        server.get_player_ids.return_value = list(player_ids)
        request = mock.Mock()
        request.rel_url.query = query
        return GameServerSync.get_view_player_ids(server, request)

    def test_an_ordinary_client_gets_the_players_it_named(self):
        self.assertEqual(self.Ask({'p': '2'}), [2])

    def test_a_hot_seat_browser_stands_in_for_the_whole_table(self):
        self.assertEqual(self.Ask({'hot_seat': ''}), [0, 1, 2])

    def test_so_does_a_spectator(self):
        self.assertEqual(self.Ask({'watch': ''}), [0, 1, 2])


if __name__ == '__main__':
    unittest.main()
