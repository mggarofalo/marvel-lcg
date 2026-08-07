from typing import Final, TypeAlias
from core import *
from game.effect import *
from game.card.face import *
from game.selector import *
from game.message import *
from game.selector.selector_rule import SelectorRule
from game.selector.selector_where import SelectorWhere
from game.selector.selector_range import SelectorRange

CATEGORY_NAME = "SELECTOR"

class SelectorTarget:

    TARGET_TYPE: TypeAlias = Literal[
        "This",
        "DefendingCharacter", "BoostForCard", "DamageTarget",
        "Attacker", "Attacked", "Attached",
        "Initiator", "Trigger",
        "Targets",
        "ToPlayer",
        "PlayedCard",

        "TeamUp",
        "ThisAttachedHereCard",
        "VillainAndEngagedSamePlayerMinion",
        "VillainAndYouEngagedMinion",

        "UsedToPayForThis",

        # "OnFieldStatusCard", # See "32007"
        # "YouRandomHandCard", # see "11022"
    ]

    TARGET_TYPE_HELPER: TypeAlias = Literal[
        "Hero|Ally", "Hero|Villain", "Ally|Minion", "Upgrade|Support", "Ally|Support", "Enemy|Scheme", "Leader|Minion",
        "Minion", "Villain", "EncounterSideScheme", "MainScheme", "PlayerSideScheme",
        "Upgrade", "Support", "Attachment", "Environment", "Obligation",
        "Identity", "Ally", "Hero", "AlterEgo", "StatusCard",
        "Friend", "Scheme2",
        "YourSuitFormUpgrade", "YourHeroTough", "YourHandCards", "YourLikeHandCards", 
        "TuckHereCard", "TuckUnderYourIdentityCard",
        "EnemyLeader", "YourLeader", 
    ]

    TARGET_TYPE_HELPER_LIST: List[TARGET_TYPE_HELPER] = Types.LiteralToList(TARGET_TYPE_HELPER)

    @staticmethod
    def IsHelpType(target: str) -> TypeGuard['SELECT.TARGET_TYPE_HELPER']:
        return target in SelectorTarget.TARGET_TYPE_HELPER_LIST

    def __init__(self,
                target: 'SELECT.TARGET_TYPE|SELECT.TARGET_TYPE_HELPER|CardFinderHelper.FINDER_TARGET|None', # If `card_type` then means "InPlay", else use `faces` or `select`) -> None:
                faces: Sequence['CardFace']|None,
                get_targets_fn: Callable[['Effect'], Sequence['CardFace']]|None,
                from_where: List['SELECT.WHERE'],
                ) -> None:
        from game.selector.selector_target_helper import TARGET_MAP_FUNC
        # provided_count = sum(param is not None for param in (target, faces, get_targets_fn))
        # assert provided_count <= 1, "Only one of target, faces, or get_targets_fn should be provided."

        self.faces = faces
        self.get_targets_fn = get_targets_fn
        self.selector_where = SelectorWhere(from_where)

        self.raw_target: Final = target

        if target in TARGET_MAP_FUNC:
            assert self.get_targets_fn == None
            self.get_targets_fn = TARGET_MAP_FUNC[target]

    def GetForcedRangeRule(self):
        TARGET_MAP_RANGE: Dict[
            'SELECT.TARGET_TYPE',
            Tuple['SELECT.RANGE_TYPE', 'SelectorRule.RULE_FOR_UI|SelectorRule.RULE_BASE']
        ] = {
            "TeamUp":                               ((2,2), "DifferentCards"),
            "VillainAndEngagedSamePlayerMinion":    ("All", "VillainAndMinionsEngagedSamePlayer"),
            "VillainAndYouEngagedMinion":           ("All", "VillainAndMinionsEngagedWithYou"),
        }
        if self.raw_target in TARGET_MAP_RANGE:
            return TARGET_MAP_RANGE[self.raw_target]
        return None

    def GetAllTarget(self, effect: 'Effect', selector_range: 'SelectorRange') -> List['CardFace']:
        found_faces: List['CardFace'] = []

        if self.faces:
            found_faces += self.faces

        from_where = self.selector_where.Get(effect, selector_range)

        if self.get_targets_fn:
            if self.selector_where.is_not_set:
                found_faces += self.get_targets_fn(effect)
            else:
                found_faces += [x for x in self.get_targets_fn(effect) if x in from_where]

        if self.faces == None and self.get_targets_fn == None:
            found_faces += from_where

        # We remove this for fixing search effect e.g. "36020"
        # if self.card_type:
        #     found_faces = [x for x in found_faces if x.IsTypeOld(self.card_type)]

        return found_faces

