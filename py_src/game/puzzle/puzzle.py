from core import *
from game.card.face import *
from game.card import *
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

        Zone groups are searched **board first**: a puzzle command that names a
        card in play means that card. Anything else duplicates it -- the field
        used to go unsearched entirely, so `Puzzle.Damage("01094", 3)` against
        the villain in play left it at full health and put a damaged second
        Rhino in the aside deck (MARVEL-51).

        Within a group, more than one match is an error naming every candidate.
        The groups are deliberately coarse for the same reason: a sub-ordering
        between the hand and the deck would be the only thing deciding which
        copy a bare name meant, and nothing the author wrote would say.

        No match at all is not an error. `FindOrCreateFace` still generates the
        card, which is how a puzzle puts something on a board that does not hold
        it yet.
        """
        player = self.world.GetCurrentPlayer()
        encounter = Worlds.GetEncounterDeckCards(self.world) + \
                    Worlds.GetEncounterDiscardPileCards(self.world)

        groups: List[Tuple[str, List['CardFace']]] = [
            ("in play",
             self.world.FindCardsOnField(name=name)),
            ("in the player's hand, deck or discard pile",
             player.hand_cards.FindCards(name=name)
             + player.player_deck.FindCards(name=name)
             + player.discard_pile.FindCards(name=name)),
            ("in the encounter deck or discard pile",
             [face for face in encounter if face.IsName(name)]),
        ]

        for where, matched in groups:
            found = self.UniqueFaces(matched)
            if len(found) > 1:
                raise PuzzleCardError(self.AmbiguityMessage(name, where, found))
            if found:
                return found[0]
        return None

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

        A field search unions board areas with the inventory and placed-card
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

