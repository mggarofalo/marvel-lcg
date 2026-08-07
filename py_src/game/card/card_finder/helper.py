from typing import TypeAlias
from core import *
from game.ability import *
from game.card.face import *

# CardFinderUtils
class CardFinderHelper:

    FINDER_TARGET: TypeAlias = Literal[
        "Players", "OtherPlayers", "FirstPlayer",
        "YourHero", "YourIdentity", "YourAlterEgo",
        "YourSidekick", "YouControlUnit", "YourAlly",
        "YouControlCards",
        "YourEngagedMinions",
    ]
    FINDER_TARGET_LIST: List[FINDER_TARGET] = Types.LiteralToList(FINDER_TARGET)

    @staticmethod
    def IsFinderTarget(target: str) -> TypeGuard['CardFinderHelper.FINDER_TARGET']:
        return target in CardFinderHelper.FINDER_TARGET_LIST

    @staticmethod
    def GetTargetType(target: 'CardFinderHelper.FINDER_TARGET') -> Type['CardFace']:
        from game.card.face.card_type import Identity
        from game.card.face.card_type import Hero
        from game.card.face.card_type import AlterEgo
        from game.card.face.card_type import Ally
        from game.card.face.card_type import StatusCard
        from game.card.face.card_face import CardFace
        from game.card.face.base import Villain
        if target in ["Players", "OtherPlayers", "FirstPlayer", "YourIdentity"]:
            return Identity
        if target in ["YourHero"]:
            return Hero
        if target in ["YourAlterEgo"]:
            return AlterEgo
        if target in ["YourAlly"]:
            return Ally
        if target in ["YourEngagedMinions"]:
            return Minion
        if target in ["OnFieldStatusCard"]:
            return StatusCard
        if target in ["VillainAndEngagedSamePlayerMinion", "VillainAndYouEngagedMinion"]:
            return Villain|Minion # type: ignore
        return CardFace

    # @staticmethod
    # def From(target: 'FINDER_TARGET'):
    #     from game.operate.worlds import Worlds
    #     from game.selector import Select
    #     from game.operate.worlds import Worlds
    #     from game.card.card_finder import CardFinder
    #     from game.card.face.card_type import Identity
    #     from game.card.face.card_type import Hero
    #     from game.card.face.card_type import AlterEgo
    #     from game.card.face.card_type import Ally
    #     from game.card.face.card_type import Minion
    #     from game.card.face.card_type import Upgrade
    #     from game.card.face.base import Unit2

    #     if target == "Players":
    #         return CardFinder(card_type=Identity)
    #     elif target == "OtherPlayers":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face != effect.this.GetControlByPlayer().GetIdentity()
    #         return CardFinder(card_type=Identity, check_effect_fn=check)
    #     elif target == "FirstPlayer":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face == Worlds.GetFirstPlayer(effect).GetIdentity()
    #         return CardFinder(card_type=Identity, check_effect_fn=check)

    #     elif target == "YourHero":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face == Select.GetYou(effect).GetIdentity()
    #         return CardFinder(card_type=Hero, check_effect_fn=check)
    #     elif target == "YourIdentity":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face == Select.GetYou(effect).GetIdentity()
    #         return CardFinder(card_type=Identity, check_effect_fn=check)
    #     elif target == "YourAlterEgo":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face == Select.GetYou(effect).GetIdentity()
    #         return CardFinder(card_type=AlterEgo, check_effect_fn=check)

    #     elif target == "YourSidekick":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face.GetControlBy() == Select.GetYou(effect) and \
    #                 face.GetInventoryDeck().FindCard(name="Sidekick", card_type=Upgrade) != None
    #         return CardFinder(card_type=Ally, check_effect_fn=check)

    #     elif target == "YouControlUnit":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face.GetControlBy() == Select.GetYou(effect)
    #         return CardFinder(card_type=Unit2, check_effect_fn=check)
    #     elif target == "YouControlCards":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face.GetControlBy() == Select.GetYou(effect)
    #         return CardFinder(card_type=Unit2, check_effect_fn=check)
    #     elif target == "YourAlly":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face.GetControlBy() == Select.GetYou(effect)
    #         return CardFinder(card_type=Ally, check_effect_fn=check)

    #     elif target == "YourEngagedMinions":
    #         def check(effect: 'Effect', face: 'CardFace'):
    #             return face.CastTo(Minion).GetEngagedPlayer() == Select.GetYou(effect)
    #         return CardFinder(card_type=Minion, check_effect_fn=check)

    #     else:
    #         assert False, f"{target}"

