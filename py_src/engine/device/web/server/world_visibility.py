"""Cut a `WorldDescriptor` down to what one client is entitled to see.

`ToDescriptor.World` builds **one** descriptor per render, for the whole table:
a `CardDescriptor` for every card in the game, each carrying `card_id`, `name`,
`info` and the current face, alongside a `visible_for_players` list saying who
may look at it. Until MARVEL-62 that whole thing went to whichever client asked
for it and `visible_for_players` was enforced in the browser -- by
`CardDescriptor.isVisible` in `public/js/marvel/descriptor.ts`. So a `curl` with
a valid `app_version` cookie read the encounter deck in order, every other
player's hand, and the identity of every face-down card in play.

This module moves that decision to the server. `RedactForViewers` takes the
shared descriptor and the player ids the request asked for, and returns a copy
in which every card those players may not see has had its **face** removed.

Two things it deliberately does not do:

- **It redacts rather than deletes.** A client still has to draw a face-down
  encounter deck of the right height and animate the right card off the top, so
  the card stays in the list with its object id, its zone and its printed back.
- **It does not authenticate.** `p`, `hot_seat` and `watch` are asserted by the
  requesting client and nothing checks them, exactly as they were before. What
  changes is where enforcement lives: with the filter server-side, adding a
  per-player token later is a change to `get_view_player_ids` alone. See
  MARVEL-145.

What survives redaction is everything a player can see across the table without
reading the card: where it sits, whether it is exhausted, what it is bound to,
and the back that is facing up. What goes is everything printed on the hidden
face, and everything derived from it -- including the links to the abilities the
card carries and to the cards it is affecting, because a card you cannot see
must not answer questions about itself.

`revision` goes too. It is a hash over the card's render info -- deliberately
computed from face-up-guarded state for the digest's sake, but still a value
that changes when the hidden face changes, which is more than a client should be
able to observe. The browser's own change detection hashes the descriptor it
received (`Lib.object.hashObjectFast`), so it keeps working on the redacted one.

The walk is driven by the shape of the data rather than a list of zone names, so
a zone added to `WorldDescriptor` later is filtered the day it is added instead
of leaking until somebody remembers this file. Every container a card could be
declared in is walked -- list, tuple, dict -- not only the `List` the descriptor
happens to use today. `unit_test/test_world_visibility.py` pins both.

**What this does not cover: the descriptor's free text.** `prompt`,
`prompt_last_text` and `event_name` are strings composed by the engine's message
senders, and some of them name cards -- `AfterPlayerGainCards_Text` formats
"{player} gains {faces}" for cards that go into a hand. Those go to every viewer
unfiltered. It is a different channel with a different fix (report counts, the
way the draw message already does), and it is MARVEL-152.
"""

from core import *
from dataclasses import replace

CATEGORY_NAME = "WEB"

# Everything printed on the card's hidden face. `replace` needs a fresh mutable
# per card, so this is a factory rather than a dict constant.
def BlankFace() -> Dict[str, Any]:
    return {
        'name'                  : '',
        'card_id'               : '',
        'pic_id'                : '',
        'card_type'             : '',
        'info'                  : {},
        'traits'                : {},
        'buffs'                 : {},
        'cost'                  : 0,
        'revision'              : 0,
        'is_new'                : False,
        'is_action'             : False,
        # Links to this card's own abilities. A card you cannot see offers you
        # no abilities, and the count of them is itself a hint at its identity.
        'effects'               : [],
        'resources'             : [],
        # What this card is affecting, and what is affecting it. Same reasoning:
        # hovering a face-down card must not light up what it points at. The
        # cards on the other end keep their own lists, so an arrow between a
        # hidden card and a visible one still draws from the visible side.
        'effect_by_cards'       : [],
        'effect_to_cards'       : [],
        # The enforcement signal. Emptied so the browser draws the back, and so
        # that "player 2 peeked at this" is not readable off the wire either.
        'visible_for_players'   : [],
    }


def IsCardDescriptor(value: Any) -> bool:
    """A `CardDescriptor`, identified by shape rather than by import.

    `engine` sits below `game` in the layering, so this module names no `game`
    type at runtime.
    """
    return is_dataclass(value) and not isinstance(value, type) \
        and hasattr(value, 'visible_for_players')


def IsVisibleTo(card: Any, viewers: Set[int]) -> bool:
    return any(player_id in viewers for player_id in card.visible_for_players)


def RedactCard(card: Any) -> Any:
    """The same card with its face removed.

    `down_card_ids` stays. It is the *printed back* -- `'player'`,
    `'encounter'`, `'villain'` or an evidence back for a single-faced card, and
    the reverse face for a double-faced one, which is the side physically
    turned up on the table. Blanking it would leave the browser with no image to
    draw for any face-down card.
    """
    return replace(card, **BlankFace())


def ResolveViewers(descriptor: Any, player_ids: 'Sequence[int]') -> Set[int]:
    """The set of players whose view this request is answered with.

    An eliminated player's client already reveals the whole board
    (`descriptor.ts`, `isVisible`), so an eliminated viewer is widened to the
    table. Everyone else sees exactly the players they asked for.
    """
    viewers = set(player_ids)
    players = getattr(descriptor, 'players', [])
    for player_id in player_ids:
        if 0 <= player_id < len(players) and players[player_id].is_eliminated:
            return viewers | set(range(len(players)))
    return viewers


def RedactCardList(cards: 'Sequence[Any]', viewers: Set[int]) -> 'Sequence[Any]':
    """Redact the hidden cards in one zone, and canonicalise their order.

    Order is half the leak. Handing back the encounter deck with its faces
    removed still says *which object id is on top*, and object ids are stable
    for the whole game -- so a card seen face-up once and shuffled back in
    would be trackable through the deck. The hidden entries are therefore
    re-emitted sorted by object id, which is a fixed order that carries no
    information about the shuffle.

    Visible cards keep their positions, so a zone where only the top card has
    been revealed still reads correctly.

    What this does not close: the ids themselves are still real, so a client
    that once saw a card face up can tell which zone it is in now. That is
    MARVEL-146, and closing it means pseudonymous ids rather than a sort.
    """
    hidden_slots = [index for index, card in enumerate(cards)
                    if not IsVisibleTo(card, viewers)]
    if not hidden_slots:
        return cards

    in_id_order = sorted((cards[index] for index in hidden_slots), key=lambda card: card.id)
    redacted = list(cards)
    for slot, card in zip(hidden_slots, in_id_order):
        redacted[slot] = RedactCard(card)
    return redacted


def RedactValue(value: Any, viewers: Set[int]) -> Any:
    """Returns `value` itself when nothing under it changed, so callers can
    tell an untouched branch by identity and leave it alone.

    Every container the descriptor could hold a card in is handled, not just
    the `List` it happens to use today: a zone declared as a tuple, a set or a
    dict would otherwise fall through to the last line and go out intact, and
    the whole point of walking the shape is that a new zone is covered before
    anyone notices it exists.
    """
    if IsCardDescriptor(value):
        return value if IsVisibleTo(value, viewers) else RedactCard(value)

    if is_dataclass(value) and not isinstance(value, type):
        return RedactDataclass(value, viewers)

    if isinstance(value, (list, tuple)):
        # Order only means something in a sequence, so this is the one place
        # the hidden entries get sorted.
        if value and IsCardDescriptor(value[0]):
            redacted: Any = RedactCardList(value, viewers)
        else:
            redacted = [RedactValue(item, viewers) for item in value]
            if all(new is old for new, old in zip(redacted, value)):
                redacted = value
        if redacted is value:
            return value
        return tuple(redacted) if isinstance(value, tuple) else list(redacted)

    if isinstance(value, dict):
        by_key = {key: RedactValue(item, viewers) for key, item in value.items()}
        if all(new is value[key] for key, new in by_key.items()):
            return value
        return by_key

    return value


def RedactDataclass(node: Any, viewers: Set[int]) -> Any:
    changes: Dict[str, Any] = {}
    for descriptor_field in fields(node):
        old = getattr(node, descriptor_field.name)
        new = RedactValue(old, viewers)
        if new is not old:
            changes[descriptor_field.name] = new
    if not changes:
        return node
    return replace(node, **changes)


def RedactForViewers(descriptor: Any, player_ids: 'Sequence[int]') -> Any:
    """The descriptor these players may have. The shared one is never mutated.

    `WorldRender.descriptor` is built once per render and read by every client,
    so this returns a copy and leaves the original alone.
    """
    return RedactDataclass(descriptor, ResolveViewers(descriptor, player_ids))
