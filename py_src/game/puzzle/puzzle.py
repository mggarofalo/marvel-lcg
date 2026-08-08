from core import *
from game.card.face import *
from game.card import *
from game.deck import DeckType
from game.effect.rule import DebugRule
from game.world import *
from game.card.face.card_type import Identity
from game.card.face.base import Unit2
from game.card.face.base import Scheme2
from game.operate.worlds import Worlds

CATEGORY_NAME = "PUZZLE"

class PuzzleCardError(Exception):
    """A puzzle command named a card that does not mean exactly one card.

    Raised rather than resolved, because the alternative is a puzzle that runs
    against a board its author did not write. Every assertion made afterwards is
    then about the wrong card, and nothing says so.
    """

################################################################################
# Where a puzzle command looks for the card it named.
#
# Nothing here is a Search. **Search** is a Marvel Champions rules term that this
# engine already implements, in `game.operate.search.Search`, over the same zones
# and with the same vocabulary -- `include_discard_pile`, `include_set_aside`.
# That is what card text compiles to: Rhino (II) reads "Search the encounter deck
# and discard pile for the Breakin' & Takin' side scheme" and `01095.py` calls
# `Search.EncounterCard(effect, include_discard_pile=True, ...)`.
#
# What follows resolves a *name a puzzle author typed* to the card they meant. It
# is performed by nobody, emits no game event, shuffles nothing, and is not
# visible to the game. So it is described here as resolving a name against zones,
# or looking in them, and never as searching them -- the two are close enough to
# be mistaken for each other, and widening this resolver to the set-aside area
# (MARVEL-61) reads exactly like adding a card effect if the word slips.

# The play-rule sentinel, printed name "rule". `ObjectManager` starts the card
# counter at -1 and `World.Initialize` registers the play rule first, so this is
# always the first card allocated and always id 0. It is engine bookkeeping, not
# a card anyone may name; `tools/spec/resolve.py` excludes the same id.
PLAY_RULE_OBJECT_ID = 0

IN_PLAY         = "in play"
PLAYER          = "in a player's hand, deck or discard pile"
ENCOUNTER       = "in the encounter deck or discard pile"
SET_ASIDE       = "set aside"
OUT_OF_THE_GAME = "out of the game"

# Every `DeckType` the engine has, assigned to exactly one group.
#
# The map is the point, not a convenience. MARVEL-51 gave the resolver three
# hand-written zone groups and MARVEL-61 was everything they missed -- the aside
# deck, the set-aside decks, the victory display, the removed-from-game area.
# Listing zones by hand is what let those be missed silently, so the zones are
# derived from `DeckType` instead and `unit_test.test_puzzle` asserts this map
# covers it exhaustively. A zone added later cannot go unconsidered: a new
# `DeckType` fails that test, and a new `Deck2` built from an existing one is
# already placed.
#
# Three things about this table are not guessable from the attribute names:
#
# - `player.set_aside_deck` *is* `player.additional_discard_pile` (`player.py`),
#   one object under two names, so `AdditionalDiscardPile` covers both.
# - `world.area_status_cards` is declared `DeckType.RemovedArea`, not
#   `StatusArea`. `StatusArea` is the per-card status component.
# - `EvidenceArea` and `RuleArea` are `is_in_play` but are *not* among the areas
#   `WorldFind.FindCardsOnField` walks. That is why `FindFaceByName` unions this
#   map's `IN_PLAY` bucket with that lookup rather than deferring to it.
ZONE_GROUP_BY_DECK_TYPE: 'Dict[DeckType, str]' = {
    # On the board, or hanging off something on it.
    DeckType.PlaceCardArea:             IN_PLAY,
    DeckType.UpgradesArea:              IN_PLAY,
    DeckType.AlliesArea:                IN_PLAY,
    DeckType.SupportsArea:              IN_PLAY,
    DeckType.EngagedEnemiesArea:        IN_PLAY,
    DeckType.HeroArea:                  IN_PLAY,
    DeckType.ObligationsArea:           IN_PLAY,
    DeckType.MainSchemesArea:           IN_PLAY,
    DeckType.SideSchemesArea:           IN_PLAY,
    DeckType.VillainArea:               IN_PLAY,
    DeckType.EnvironmentArea:           IN_PLAY,
    DeckType.EvidenceArea:              IN_PLAY,
    DeckType.RuleArea:                  IN_PLAY,
    # The player's own three.
    DeckType.HandsArea:                 PLAYER,
    DeckType.PlayerDeck:                PLAYER,
    DeckType.DiscardPile:               PLAYER,
    # The encounter deck and what has been through it.
    DeckType.EncounterDeck:             ENCOUNTER,
    DeckType.EncounterDiscardPile:      ENCOUNTER,
    # Held by the game but not in play: set aside at setup, staged for later,
    # or passing through mid-resolution.
    DeckType.AsideDeck:                 SET_ASIDE,
    DeckType.AdditionalDeck:            SET_ASIDE,
    DeckType.AdditionalDiscardPile:     SET_ASIDE,
    DeckType.DealtEncounterCardsDeck:   SET_ASIDE,
    DeckType.BoostingArea:              SET_ASIDE,
    DeckType.BoostCardsDeck:            SET_ASIDE,
    DeckType.ProcessingArea:            SET_ASIDE,
    DeckType.RevealingArea:             SET_ASIDE,
    DeckType.ResourcesArea:             SET_ASIDE,
    DeckType.MainSchemesDeck:           SET_ASIDE,
    DeckType.VillainDeck:               SET_ASIDE,
    # Gone. Tried last, and only so that naming one of these cards acts on it
    # rather than quietly building a fresh copy.
    DeckType.RemovedArea:               OUT_OF_THE_GAME,
    DeckType.VictoryDisplay:            OUT_OF_THE_GAME,
    DeckType.StatusArea:                OUT_OF_THE_GAME,
}

class RunPuzzle:

    def __init__(self, world: 'World') -> None:
        self.world = world
        self.debug_rule = DebugRule(world.object_manager.card_dict[0].face)

    CardName = str|Card|CardFace

    def FindOrCreateFace(self, card: CardName) -> 'CardFace':
        if isinstance(card, CardFace):
            return card.card.face
        elif isinstance(card, Card):
            return card.face
        elif type(card) is int:
            return self.world.object_manager.card_dict[card].face
        elif type(card) is str:
            found_card = self.FindFaceByName(card)
            if found_card:
                return found_card
        else:
            assert False, f"{card=}"
        return self.CreateCard(card)

    def FindFaceByName(self, name: str) -> 'CardFace|None':
        """The one card `name` means, or None if the game does not hold it yet.

        The name is resolved against every zone the game holds, group by group
        in the order listed below. (Against, not by searching them -- see the
        note above `PLAY_RULE_OBJECT_ID` for why that word is reserved.) That
        completeness is not a convenience: each zone left out is a `Puzzle.*`
        command that silently acts on a duplicate instead of the card the
        author named, which is what `Puzzle.Damage("01094", 3)` did against the
        villain in play before MARVEL-51 -- left it at full health and put a
        damaged second Rhino in the aside deck. MARVEL-61 was the same failure
        in the zones MARVEL-51 did not reach. So the groups are derived from
        `ZONE_GROUP_BY_DECK_TYPE` rather than hand-listed.

        **Board first**: a command that names a card in play means that card.
        Then the player's own zones, then the encounter deck, then what is set
        aside, and last what has left the game.

        Within a group, more than one match is an error naming every candidate.
        The groups are deliberately coarse for that reason: a sub-ordering
        between the hand and the deck would be the only thing deciding which
        copy a bare name meant, and nothing the author wrote would say. Set
        aside and out of the game are two groups rather than one *because*
        something does say -- a card in the victory display or removed from the
        game is gone, a set-aside card is still in the game and can come back.

        The widening has a consequence worth stating: `Gain`, `Reveal`,
        `PutIntoPlay` and `Deal` want a card that is not in play, and they now
        resolve to a set-aside copy where they used to build one. That is the
        better reading -- a puzzle naming a card the scenario set aside means
        that card -- and it is the same trade MARVEL-51 already made for the
        board, the hand and the encounter deck.

        No match at all is still not an error. `FindOrCreateFace` generates the
        card, which is how a puzzle puts something on a board that does not
        hold it yet. What it generates lands in `world.aside_deck`, which the
        `set aside` group now covers, so a second command naming the same card
        gets the copy the first one made instead of building another.
        """
        player = self.world.GetCurrentPlayer()
        encounter = Worlds.GetEncounterDeckCards(self.world) + \
                    Worlds.GetEncounterDiscardPileCards(self.world)
        held = self.FacesByZoneGroup(name)

        # The first three groups keep their own lookups. They are what
        # MARVEL-51 settled, and `FindCardsOnField` reaches upgrades and tucked
        # cards through the decks hanging off a board card. `held` adds what
        # those lookups do not reach; `UniqueFaces` absorbs the overlap.
        #
        # `held` is not scoped to the current player, so in a multiplayer game
        # the second group is every player's hand, deck and discard rather than
        # only the current player's. Deliberate: a card another player holds is
        # a card the game holds, and the alternative is the same silent
        # duplicate this resolver exists to stop. Two players holding a copy is
        # then an ambiguity, which is the honest answer -- a bare name does not
        # say whose.
        groups: List[Tuple[str, List['CardFace']]] = [
            (IN_PLAY,
             self.world.FindCardsOnField(name=name)
             + held[IN_PLAY]),
            (PLAYER,
             player.hand_cards.FindCards(name=name)
             + player.player_deck.FindCards(name=name)
             + player.discard_pile.FindCards(name=name)
             + held[PLAYER]),
            (ENCOUNTER,
             [face for face in encounter if face.IsName(name)]
             + held[ENCOUNTER]),
            (SET_ASIDE,
             held[SET_ASIDE]),
            (OUT_OF_THE_GAME,
             held[OUT_OF_THE_GAME]),
        ]

        for where, matched in groups:
            found = self.UniqueFaces(matched)
            if len(found) > 1:
                raise PuzzleCardError(self.AmbiguityMessage(name, where, found))
            if found:
                return found[0]
        return None

    def FacesByZoneGroup(self, name: str) -> 'Dict[str, List[CardFace]]':
        """Cards matching `name`, bucketed by the zone group holding them.

        Walks `world.object_manager.card_dict`, which holds every card the game
        has made, and reads `card.area` for where each one is now. That is the
        same completeness `tools/spec/resolve.py` relies on, and the reason a
        zone cannot be missed here: there is no list of zones to fall behind.

        Two things in `card_dict` are not cards a puzzle may name.

        Object id 0 is the engine's play-rule sentinel, printed name "rule",
        created by `World.Initialize` before anything else and parked in the
        removed area -- `RunPuzzle.__init__` builds `self.debug_rule` out of it.
        The removed area is newly reachable here, so without this guard
        `Puzzle.Damage("rule", 3)` would silently resolve against the engine's
        own bookkeeping instead of building a card. `resolve.py` excludes the
        same id for the same reason.

        Cards sitting in `area.removed_cards` are skipped too. That list is
        where a detaching card waits (`CanAttach.DetachFrom2`), not a zone, and
        `Deck.FindCards` does not return them either.

        Iteration is over `card_dict`, so bucket order is card creation order --
        which is what `AmbiguityMessage` re-sorts by object id anyway, and not
        a `set`, so nothing here depends on hash order.
        """
        held: 'Dict[str, List[CardFace]]' = {
            IN_PLAY: [], PLAYER: [], ENCOUNTER: [],
            SET_ASIDE: [], OUT_OF_THE_GAME: [],
        }
        for object_id, card in self.world.object_manager.card_dict.items():
            if object_id == PLAY_RULE_OBJECT_ID:
                continue
            area = getattr(card, "area", None)
            if area is None or card in area.removed_cards:
                continue
            face = card.face
            if face.IsName(name):
                held[ZONE_GROUP_BY_DECK_TYPE[area.deck_type]].append(face)
        return held

    @staticmethod
    def AmbiguityMessage(name: str, where: str, found: Sequence['CardFace']) -> str:
        """Name every candidate, and the object id that would pick one.

        `PuzzleHelper.Exec` binds `c<N>` for every card in the game before it
        runs a command, so the object id is an escape hatch the author already
        has -- worth saying, since the alternative reading of this error is that
        the command cannot be written at all.

        Candidates are listed by object id rather than in the order the zones
        yielded them: deck order puts the most recently created card on top and
        moves under a shuffle, and an error message should not be the one thing
        in the engine that a shuffle rewords.
        """
        ordered = sorted(found, key=lambda face: face.card.object_id)
        # `no_hidden` because the author is being told which cards their own
        # puzzle put there; whether the client may see one is a different
        # question from which one they meant.
        candidates = ", ".join(
            face.GetDisplayName(no_hidden=True) for face in ordered)
        ids = ", ".join(f"c{face.card.object_id}" for face in ordered)
        return (f"{name!r} matches {len(ordered)} cards {where}: {candidates}. "
                f"Name one by object id ({ids}) instead.")

    @staticmethod
    def UniqueFaces(faces: Sequence['CardFace']) -> List['CardFace']:
        """One entry per card, in the order first seen.

        A field lookup unions board areas with the inventory and placed-card
        decks hanging off them, so the same card can be reached twice. Counting
        area memberships instead of cards would report an ambiguity that is not
        one.
        """
        found: List['CardFace'] = []
        seen: Set[int] = set()
        for face in faces:
            object_id = face.card.object_id
            if object_id not in seen:
                seen.add(object_id)
                found.append(face)
        return found

    def CreateCard(self, card_name: str) -> 'CardFace':
        from game.card.factory import CardFactory
        from game.card.face.base import ClassCard
        card = CardFactory.GenerateCard(card_name, self.world.aside_deck, self.world)
        if ClassCard.IsType(card.face):
            player = self.world.GetCurrentPlayer()
            card.SetOwner(player)
        return card.face

    def FindCommandCards(self, *cards: CardName) -> List['CardFace']:
        faces = [self.FindOrCreateFace(x) for x in cards]
        return faces

    ################################################################################
    #
    def CreateEncounterDiscardPile(self, *cards: str):
        from game.card.factory import CardFactory
        villain = Worlds.GetAllVillains(self.world)[0]
        for card in cards:
            CardFactory.GenerateCard(card, villain.encounter_discard_pile, self.world)

    def CreateEncounterDeck(self, *cards: str):
        from game.card.factory import CardFactory
        villain = Worlds.GetAllVillains(self.world)[0]
        for card in cards:
            CardFactory.GenerateCard(card, villain.encounter_deck, self.world)

    def CreateHandCards(self, *cards: str):
        from game.card.factory import CardFactory
        first_player = self.world.GetFirstPlayer()
        for card in cards:
            CardFactory.GenerateCard(card, first_player.hand_cards, self.world)

    def CreatePlayerDiscardPile(self, *cards: str):
        from game.card.factory import CardFactory
        first_player = self.world.GetFirstPlayer()
        for card in cards:
            CardFactory.GenerateCard(card, first_player.discard_pile, self.world)

    def CreatePlayerDeck(self, *cards: str):
        from game.card.factory import CardFactory
        first_player = self.world.GetFirstPlayer()
        for card in cards:
            CardFactory.GenerateCard(card, first_player.player_deck, self.world)

    def CreatePlayerAdditionalDeck(self, *cards: str):
        from game.card.factory import CardFactory
        first_player = self.world.GetFirstPlayer()
        for card in cards:
            CardFactory.GenerateCard(card, first_player.additional_discard_pile, self.world)

    ################################################################################
    #
    def Gain(self, *cards: CardName, call_back: Callable[['CardFace'], None]|None=None):
        player = self.world.GetCurrentPlayer()
        for face in self.FindCommandCards(*cards):
            player.GainCard(face, self.debug_rule)
            if call_back:
                call_back(face)

    def Draw(self, num: int):
        player = self.world.GetCurrentPlayer()
        effect = DebugRule(player.GetIdentity())
        player.DrawUp(num, effect)

    ################################################################################
    # Card
    def Ready(self, *cards: CardName):
        from game.operate.faces import Faces
        faces = self.FindCommandCards(*cards)
        Faces.ReadyAll(faces, DebugRule(faces[0]))

    def Exhaust(self, *cards: CardName):
        from game.operate.faces import Faces
        faces = self.FindCommandCards(*cards)
        Faces.ExhaustAll(faces, DebugRule(faces[0]))

    def Discard(self, *cards: CardName):
        from game.operate.faces import Faces
        faces = self.FindCommandCards(*cards)
        Faces.DiscardAll(faces, DebugRule(faces[0]))

    def Remove(self, *cards: CardName):
        from game.operate.faces import Faces
        faces = self.FindCommandCards(*cards)
        Faces.RemoveAllFromGame(faces, DebugRule(faces[0]))

    def Flip(self, *cards: CardName):
        from game.operate.faces import Faces
        faces = self.FindCommandCards(*cards)
        Faces.FlipAllTo(faces, None, DebugRule(faces[0]))

    ################################################################################
    # Unit
    def Heal(self, card: CardName, val: int):
        face = self.FindOrCreateFace(card)
        from game.card.face.attribute.can_health import CanHealth
        if CanHealth.IsType(face):
            face.HealHealth(val, self.debug_rule)

    def Damage(self, card: CardName, val: int, source: CardFace|None=None):
        face = self.FindOrCreateFace(card)
        from game.card.face.attribute.can_health import CanHealth
        if CanHealth.IsType(face):
            if source == None:
                source = face
            face.TakeDamage(source, val, self.debug_rule)

    def Confuse(self, card: CardName):
        from game.operate.faces import Faces
        unit = self.FindOrCreateFace(card).CastTo(Unit2)
        if unit.IsConfused():
            unit.DiscardConfused(DebugRule(unit), rule=1)
        else:
            Faces.GiveStatus([unit], "Confused", DebugRule(unit))

    def Stun(self, card: CardName):
        from game.operate.faces import Faces
        unit = self.FindOrCreateFace(card).CastTo(Unit2)
        if unit.IsStunned():
            unit.DiscardStunned(DebugRule(unit), rule=1)
        else:
            Faces.GiveStatus([unit], "Stunned", DebugRule(unit))

    def Tough(self, card: CardName):
        from game.operate.faces import Faces
        unit = self.FindOrCreateFace(card).CastTo(Unit2)
        if unit.IsTough():
            unit.DiscardTough(DebugRule(unit), rule=1)
        else:
            Faces.GiveStatus([unit], "Tough", DebugRule(unit))

    def ChangeForm(self, card: CardName, rule: Literal['', 'Identity', 'Hero']=''):
        face = self.FindOrCreateFace(card)
        if Identity.IsType(face):
            if rule == 'Identity' or rule == '':
                face.ChangeToOtherIdentityForm(self.debug_rule)
            if rule == 'Hero':
                face.ChangeToOtherHeroForm(self.debug_rule)

    ################################################################################
    #
    def PlaceThreat(self, card: CardName, val:int):
        face = self.FindOrCreateFace(card).CastTo(Scheme2)
        if val > 0:
            face.PlaceThreatInternal(val, self.debug_rule)
        elif val < 0:
            val = -1 * val
            face.RemoveThreatFromSchemes([face], val, self.debug_rule)

    def SetThreat(self, card: CardName, val:int):
        face = self.FindOrCreateFace(card).CastTo(Scheme2)
        self.PlaceThreat(face, val - face.threat)

    ################################################################################
    #
    def Counter(self, card: CardName, name: 'CardFace.COUNTER', size: int=3):
        from game.card.face.attribute.can_place_counter import CanPlaceCounter
        from game.operate.faces import Faces

        face = self.FindOrCreateFace(card)
        if isinstance(face, CanPlaceCounter):
            if size > 0:
                Faces.PlaceCountersOn([face], size, name, self.debug_rule)
            else:
                Faces.RemoveCountersOn([face], -1 * size, name, self.debug_rule)

    def Token(self, card: CardName, name: 'CardFace.TOKEN', size: int=3):
        from game.card.face.attribute.can_place_token import CanPlaceToken
        from game.operate.faces import Faces

        face = self.FindOrCreateFace(card)
        if isinstance(face, CanPlaceToken):
            if size > 0:
                Faces.PlaceTokensOn([face], size, name, self.debug_rule)
            else:
                Faces.RemoveTokensOn([face], size, name, self.debug_rule)

    ################################################################################
    #
    def Reveal(self, card: CardName, *, call_back: Callable[['CardFace'], None]|None=None) -> 'CardFace':
        face = self.FindOrCreateFace(card)
        player = self.world.GetCurrentPlayer()
        face.Reveal(player, self.debug_rule)
        if call_back:
            call_back(face)
        return face

    def PutIntoPlay(self, card: CardName) -> 'CardFace':
            face = self.FindOrCreateFace(card)
            player = self.world.GetCurrentPlayer()
            face.PutIntoPlay(player, self.debug_rule)
            return face

    def Boost(self, card: CardName) -> 'CardFace':
        from game.operate.worlds import Worlds
        face = self.FindOrCreateFace(card)
        card = face.card

        player = self.world.GetCurrentPlayer()
        villain = Worlds.FindVillainByGameArea(player.GetIdentity().card.game_area)

        if villain:
            card.MoveToArea(villain.encounter_deck, self.debug_rule)
            card.MoveToTop(card.area, self.debug_rule)
            villain.DoActivate(player, self.debug_rule)
        return face

    def Deal(self, card: CardName) -> 'CardFace':
        face = self.FindOrCreateFace(card)
        card = face.card

        player = self.world.GetCurrentPlayer()
        effect = DebugRule(player.GetIdentity())
        player.DealEncounterCard(face, effect)
        return face

    ################################################################################
    #
    def PutIntoDeck(self, *cards: CardName):
        from game.operate.worlds import Worlds
        from game.operate.faces import Faces
        for face in self.FindCommandCards(*cards):
            player = self.world.GetCurrentPlayer()
            if face.IsInDeck() and not face.card.area.flags.is_discards:
                Faces.MoveAllToDeck([face], face.card.area, "Top", self.debug_rule)
            elif face.GetOwner() == player:
                Faces.MoveAllTo([face], player.player_deck, self.debug_rule)
            else:
                villain = Worlds.FindVillainByGameArea(player.GetIdentity().card.game_area)
                if villain:
                    Faces.MoveAllTo([face], villain.encounter_deck, self.debug_rule)

    ################################################################################
    #
    def End(self, message: str=""):
        from engine import Engine
        game = Engine.game
        from engine.log import Notify
        controller_manager = game.controller_manager
        controller_manager.skip.SetIsSkipping(False)
        if message:
            Notify.Game(message)

class PuzzleHelper:

    ################################################################################
    #
    # @staticmethod
    # def Exec(commands: List[str]):
    #     for c in ObjectManager.card_dict:
    #         exec(f'c{c} = ObjectManager.card_dict[{c}].face')
    #     for command in commands:
    #         exec(command)

    @staticmethod
    def Exec(commands: List[str], world: 'World'):
        # from engine.lib import Log
        import ast

        Puzzle = RunPuzzle(world)
        Unused(Puzzle)

        class PuzzleCallVisitor(ast.NodeVisitor):
            def visit_Call(self, node: ast.Call):
                # Check if the function call is to a static method of Puzzle
                if isinstance(node.func, ast.Attribute) and isinstance(node.func.value, ast.Name):
                    if node.func.value.id != 'Puzzle':
                        raise ValueError(f"Function call to '{node.func.attr}' is not allowed. Only calls to 'Puzzle' methods are permitted.")
                else:
                    raise ValueError("Function call to a non-attribute is not allowed.")

                # Continue visiting child nodes
                self.generic_visit(node)

        for command in commands:
            # Parse the command into an AST
            parsed_code = ast.parse(command)

            # Create an instance of the visitor and visit the parsed code
            visitor = PuzzleCallVisitor()

            visitor.visit(parsed_code)

            for c in world.object_manager.card_dict:
                exec(f'c{c} = world.object_manager.card_dict[{c}].face')

            exec(command)  # Only execute if the command is valid
            # try:
            #     pass
            # except ValueError as e:
            #     Log.Assert(CATEGORY_NAME, f"Error executing command '{command}': {e}")
            # except Exception as e:
            #     Log.Assert(CATEGORY_NAME, f"An error occurred while executing command '{command}': {e}")

