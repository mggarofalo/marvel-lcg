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
which cards point at it, and the back that is facing up. What goes is everything
printed on the hidden face.

`revision` goes too. It is a hash over the card's render info -- deliberately
computed from face-up-guarded state for the digest's sake, but still a value
that changes when the hidden face changes, which is more than a client should be
able to observe. The browser's own change detection hashes the descriptor it
received (`Lib.object.hashObjectFast`), so it keeps working on the redacted one.

The walk is driven by the shape of the data rather than a list of zone names, so
a zone added to `WorldDescriptor` later is filtered the day it is added instead
of leaking until somebody remembers this file. `unit_test/test_world_visibility.py`
pins that.
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
    tell an untouched branch by identity and leave it alone."""
    if IsCardDescriptor(value):
        return value if IsVisibleTo(value, viewers) else RedactCard(value)

    if is_dataclass(value) and not isinstance(value, type):
        return RedactDataclass(value, viewers)

    if isinstance(value, list):
        items: List[Any] = value
        if items and IsCardDescriptor(items[0]):
            return RedactCardList(items, viewers)
        rebuilt = [RedactValue(item, viewers) for item in items]
        changed = any(new is not old for new, old in zip(rebuilt, items))
        return rebuilt if changed else value

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
