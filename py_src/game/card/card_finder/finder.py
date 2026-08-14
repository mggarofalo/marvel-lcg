from typing import TypeAlias
from core import *

from game.effect import *
from game.card.face import *
from game.world.game_area import *
from game.player import *

from game.element.resources import Resources

################################################################################
#
class CardFinder(metaclass=Tracker.Meta):

    NonEliteMinion: 'CardFinder'

    class KWArgs(TypedDict, total=False):
        # Name
        ## name: "str|None"
        names: List[str]|None
        # non_name: str|None
        card_ids: List[str]|None
        # title_contain: str|None
        #  subname: str|None
        with_texts: List[Literal["Hero Action", "Hero Response"]]|None
        # Set
        set_name: "CardFace.SETNAMES|None"
        non_set_name: "CardFace.SETNAMES|None"
        #  packs: List[str]|None
        # Trait
        ## trait: "CardFace.TRAITS|None"
        traits: List["CardFace.TRAITS"]|None # OR
        # non_trait: "CardFace.TRAITS|None"
        # share_trait: 'CardFace|None'
        # Class
        card_class: "ClassCard.CARD_CLASS|None"
        card_classes: List["ClassCard.CARD_CLASS"]|None # OR, like aspect
        # deck_building_class : 'PlayerCard.DECK_BUILDING_CLASS|None'
        # Type / Form
        # card_type: 'Type[CardFace]|Any|None'
        #  card_types: List[Type['TC']]=[],
        non_card_type: 'Type[CardFace]|Any|None'
        # form: 'CardFace.FORM|None'
        # Normal
        is_unique: bool|None
        # is_permanent: bool|None
        is_face_up: bool|None
        is_temporary: bool|None
        # canbe_exhaust: bool|None
        # canbe_ready: bool|None
        # canbe_flip: bool|None
        # canbe_discard: bool|None
        # Game Area
        # game_area: 'GameArea|None'
        # Unit
        # canbe_heal: bool|None
        #  has_health: bool|None # For a character, this is by default
        # to allow "zombie" character, turn `has_non_health` on
        # has_non_health: bool|None
        # with_health: int|None
        # Status
        # is_tough: bool|None
        # is_confuse: bool|None
        is_stunned: bool|None
        # has_confuse_status: bool|None
        # canbe_tough: bool|None
        # canbe_confused: bool|None
        # canbe_stunned: bool|None
        # has_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None # OR
        # canbe_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None # OR
        # has_any_status: bool|None
        # Asset
        # canbe_attach_by: 'CardFace|None'
        canbe_attach_to: 'CardFace|None'
        # Scheme
        # has_threat: bool|None
        # Villain / Minion / Obligation
        # is_active_villain: bool|None
        stage: int|None
        is_nemesis: 'Player|None|Literal[True, False]' # Only for SideScheme and Minion
        # engaged_with: 'CardFinder|None'
        obligation_give_to: 'Player|None'
        # Icons
        amplify: bool|None
        acceleration_icon: bool|None
        acceleration_token: bool|None
        crisis: bool|None
        hazard: bool|None
        # Counter / Token
        has_counter: 'CardFace.COUNTER|Literal["all-purpose"]|None'
        # has_token: 'CardFace.TOKEN|None'
        # Attach
        # has_attach: 'CardFinder|None'
        # attach_to: 'CardFinder|None'
        with_attach: 'bool|CardFinder|Type[CardFace]|None'
        # bind_to: 'CardFinder|None'
        # Cost
        # cost_equal_or_greater: int|None
        cost_equal_or_less: int|None # 3 -> [0,1,2,3]
        # Res
        has_printed_res: "Resources.RBYG|List[Resources.RBYG]|None|Literal[True]" # convert_green_res = False
        # non_printed_res: "Resources.RBYG|None"
        # with_res: "Resources.RBY|None"  # convert_green_res = True
        # Owner
        #  control_by_player: Callable[['Effect'], Player]|None
        # owner: 'Player|None'
        # Other
        is_scenario_specific: bool|None
        # not_has_that_unique_in_play: bool|None
        check_face_fn: Callable[['CardFace'], bool]|None
        # check_effect_fn: Callable[['Effect', 'CardFace'], bool]|None # Only call when has effect

    def __init__(self,
                 *,
                # Name
                 name: "str|None"=None,
                 names: List[str]|None=None,
                 subtitle: "str|None"=None,
                 non_name: str|None=None,
                 card_ids: List[str]|None=None,
                 title_contain: str|None=None,
                #  subname: str|None=None,
                 non_face: "CardFace|None"=None,
                 with_texts: List[Literal["Hero Action", "Hero Response"]]|None=None,
                 subtype: "Evidence.SUBTYPE|None"=None,
                # Set
                 set_name: "CardFace.SETNAMES|None"=None,
                 non_set_name: "CardFace.SETNAMES|None"=None,
                #  packs: List[str]|None=None,
                # Trait
                 trait: "CardFace.TRAITS|None"=None,
                 traits: List["CardFace.TRAITS"]|None=None, # OR
                 non_trait: "CardFace.TRAITS|None"=None,
                 share_trait: 'CardFace|None'=None,
                # Class
                 card_class: "ClassCard.CARD_CLASS|None"=None,
                 card_classes: List["ClassCard.CARD_CLASS"]|None=None, # OR
                 deck_building_class : 'ClassCard.DECK_BUILDING_CLASS|None'=None,
                # Type / Form
                 card_type: 'Type[TC]|Any|None'=None,
                #  card_types: List[Type['TC']]=[],
                 non_card_type: 'Type[TC]|Any|None'=None,
                 form: 'CardFace.FORM|None'=None,
                # Normal
                 is_unique: bool|None=None,
                 is_permanent: bool|None=None,
                 is_face_up: bool|None=None,
                 is_temporary: bool|None=None,
                 canbe_play: bool|None=None,
                 canbe_exhaust: bool|None=None,
                 canbe_ready: bool|None=None,
                 canbe_flip: bool|None=None,
                 canbe_discard: bool|None=None,
                # Game Area
                 game_area: 'GameArea|None'=None,
                # Unit
                 canbe_heal: bool|None=None,
                 sustained: bool|None=None,
                #  has_health: bool|None=None, # For a character, this is by default
                # to allow "zombie" character, turn `has_non_health` on
                 has_non_health: bool|None=None,
                 with_health: int|None=None,
                # Status
                 with_tough: bool|None=None,
                 is_confused: bool|None=None,
                 is_stunned: bool|None=None,
                 has_confuse_status: bool|None=None,
                 canbe_tough: bool|None=None,
                 canbe_confused: bool|None=None,
                 canbe_stunned: bool|None=None,
                 has_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None=None, # OR
                 canbe_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None=None, # OR
                #  has_any_status: bool|None=None,
                # Asset
                 canbe_attach_by: 'CardFace|None'=None,
                 canbe_attach_to: 'CardFace|None'=None,
                # Scheme
                 has_threat: bool|None=None,
                # Villain / Minion / Obligation
                 is_active_villain: bool|None=None,
                 stage: int|None=None,
                 is_nemesis: 'Player|None|Literal[True, False]'=None, # Only for SideScheme and Minion
                 engaged_with: 'CardFinder|Player|None'=None,
                 not_engaged_with: 'Player|None'=None,
                 obligation_give_to: 'Player|None'=None,
                # Icons, these are AND, not OR
                 amplify: bool|None=None,
                 acceleration_icon: bool|None=None,
                 acceleration_token: bool|None=None,
                 crisis: bool|None=None,
                 hazard: bool|None=None,
                # Counter / Token
                 has_counter: 'CardFace.COUNTER|Literal["all-purpose"]|None'=None,
                 has_token: 'CardFace.TOKEN|None'=None,
                # Attach
                 has_attach: 'CardFinder|None'=None,
                 attach_to: 'CardFinder|None'=None,
                 with_attach: 'bool|CardFinder|Type[CardFace]|None'=None,
                 not_with_attach: 'CardFinder|Type[CardFace]|None'=None,
                 bind_to: 'CardFinder|CardFace|None'=None,
                # Cost
                 cost_equal_or_greater: int|None=None,
                 cost_equal_or_less: int|None=None, # 3 -> [0,1,2,3]
                # Res
                 has_printed_res: "Resources.RBYG|List[Resources.RBYG]|None|Literal[True]"=None, # convert_green_res = False
                 non_printed_res: "Resources.RBYG|None"=None,
                 with_res: "Resources.RBY|None"=None,  # convert_green_res = True
                # Owner
                #  control_by_player: Callable[['Effect'], Player]|None=None,
                 owner: 'Player|None'=None,
                # Other
                 is_scenario_specific: bool|None=None,
                 not_has_that_unique_in_play: bool|None=None,
                 check_face_fn: Callable[['TC'], bool]|None=None,
                 check_effect_fn: Callable[['Effect', 'TC'], bool]|None=None, # Only call when has effect
                 ) -> None:
        from game.card.face.card_face import CardFace
        if card_type == CardFace or card_type == 'CardFace': # Hack for `CardFaceType`
            card_type = None

        self.name: "str|None" = name
        self.names: List[str] = names if names else []
        self.non_name: str|None = non_name
        self.subtitle: "str|None" = subtitle
        self.card_ids: List[str] = card_ids if card_ids else []
        self.title_contain: str|None = title_contain
        self.non_face: "CardFace|None" = non_face
        self.with_texts: List[Literal["Hero Action", "Hero Response"]] = with_texts if with_texts else []
        self.subtype: "Evidence.SUBTYPE|None" = subtype
        self.set_name: "CardFace.SETNAMES|None" = set_name
        self.non_set_name: "CardFace.SETNAMES|None" = non_set_name
        self.trait: "CardFace.TRAITS|None" = trait
        self.traits: List["CardFace.TRAITS"] = traits if traits else []
        self.non_trait: "CardFace.TRAITS|None" = non_trait
        self.share_trait: 'CardFace|None' = share_trait
        self.card_class: "ClassCard.CARD_CLASS|None" = card_class
        self.card_classes: List["ClassCard.CARD_CLASS"] = card_classes if card_classes else []
        self.deck_building_class : 'ClassCard.DECK_BUILDING_CLASS|None' = deck_building_class
        self.card_type: 'Type[TC]|Any|None' = card_type
        self.non_card_type: 'Type[TC]|Any|None' = non_card_type
        self.form: 'CardFace.FORM|None' = form
        self.is_unique: bool|None = is_unique
        self.is_permanent: bool|None = is_permanent
        self.is_face_up: bool|None = is_face_up
        self.is_temporary: bool|None = is_temporary
        self.canbe_play: bool|None = canbe_play
        self.canbe_exhaust: bool|None = canbe_exhaust
        self.canbe_ready: bool|None = canbe_ready
        self.canbe_flip: bool|None = canbe_flip
        self.canbe_discard: bool|None = canbe_discard
        self.game_area: 'GameArea|None' = game_area
        self.canbe_heal: bool|None = canbe_heal
        self.sustained: bool|None = sustained
        self.has_non_health: bool|None = has_non_health
        self.with_health: int|None = with_health
        self.with_tough: bool|None = with_tough
        self.is_confused: bool|None = is_confused
        self.is_stunned: bool|None = is_stunned
        self.has_confuse_status: bool|None = has_confuse_status
        self.canbe_tough: bool|None = canbe_tough
        self.canbe_confused: bool|None = canbe_confused
        self.canbe_stunned: bool|None = canbe_stunned
        self.has_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None = has_status
        self.canbe_status: 'CardFace.STATUS|Literal["Any"]'|List['CardFace.STATUS']|None = canbe_status
        # self.has_any_status: bool|None = has_any_status
        self.canbe_attach_by: 'CardFace|None' = canbe_attach_by
        self.canbe_attach_to: 'CardFace|None' = canbe_attach_to
        self.has_threat: bool|None = has_threat
        self.is_active_villain: bool|None = is_active_villain
        self.stage: int|None = stage
        self.is_nemesis: 'Player|None|Literal[True, False]' = is_nemesis
        self.engaged_with: 'CardFinder|Player|None' = engaged_with
        self.not_engaged_with: 'Player|None' = not_engaged_with
        self.obligation_give_to: 'Player|None' = obligation_give_to
        self.amplify: bool|None = amplify
        self.acceleration_icon: bool|None = acceleration_icon
        self.acceleration_token: bool|None = acceleration_token
        self.crisis: bool|None = crisis
        self.hazard: bool|None = hazard
        self.has_counter: 'CardFace.COUNTER|Literal["all-purpose"]|None' = has_counter
        self.has_token: 'CardFace.TOKEN|None' = has_token
        self.has_attach: 'CardFinder|None' = has_attach
        self.attach_to: 'CardFinder|None' = attach_to
        self.with_attach: 'bool|CardFinder|Type[CardFace]|None' = with_attach
        self.not_with_attach: 'CardFinder|Type[CardFace]|None' = not_with_attach
        self.bind_to: 'CardFinder|CardFace|None' = bind_to
        self.cost_equal_or_greater: int|None = cost_equal_or_greater
        self.cost_equal_or_less: int|None = cost_equal_or_less
        self.has_printed_res: "Resources.RBYG|List[Resources.RBYG]|None|Literal[True]" = has_printed_res
        self.non_printed_res: "Resources.RBYG|None" = non_printed_res
        self.with_res: "Resources.RBY|None" = with_res
        self.owner: 'Player|None' = owner
        self.is_scenario_specific = is_scenario_specific
        self.not_has_that_unique_in_play: bool|None = not_has_that_unique_in_play
        self.check_face_fns: List[Callable[['TC'], bool]] = [check_face_fn] if check_face_fn else []
        self.check_effect_fns: List[Callable[['Effect', 'TC'], bool]] = [check_effect_fn] if check_effect_fn else []
        self.or_finders: List['CardFinder'] = []

        from game.card.card_finder.checker import Check
        from game.card.card_finder.checker import Checks
        from game.card.card_finder.checker import Checks2
        CardFinder.Check = Check
        CardFinder.Checks = Checks
        CardFinder.Checks2 = Checks2

        self.is_empty = not self.params

    def Check(self, face: 'CardFace', effect: 'Effect|None'=None) -> bool:
        ...

    def Checks(self, faces: Sequence['TC'], effect: 'Effect|None'=None) -> List['TC']:
        ...

    def Checks2(self, faces: Sequence['CardFace'], card_type: Type[TF]) -> List['TF']:
        ...

    # def GetRefCount(self) -> int:
    #     import sys
    #     # Since `getrefcount` includes the temporary reference from the function call,
    #     # we subtract `3` to get the actual number of references.
    #     return sys.getrefcount(self) - 3

    ################################################################################
    #
    def __and__(self, other: 'CardFinder|None') -> 'CardFinder':
        if other == None or other.is_empty:
            return self
        if self.is_empty:
            return other
        params = self.params
        for key, value in other.params.items():
            if key in params:
                if issubclass(params[key], value):
                    value = params[key]
                elif issubclass(value, params[key]):
                   value = value
                else:
                    assert False
            setattr(self, key, value)
        return self

    def __or__(self, other: 'CardFinder|None') -> 'CardFinder':
        if other == None or other.is_empty:
            return self
        if self.is_empty:
            return other
        self.or_finders.append(other)
        return self

    @property
    def params(self) -> Dict[str, Any]:
        return {key: value for key, value in self.__dict__.items() if value != None and value != [] and key != 'is_empty'}

    def __repr__(self) -> str:
        return f"{self.params}"

    def __bool__(self) -> bool:
        return not self.is_empty

    # Hack for finder == None
    def __eq__(self, other: Any) -> bool:
        if other is None:
            return self.is_empty
        return NotImplemented

    @property
    def check_by_name(self) -> List[str]:
        names: List[str] = []
        if self.name != None:
            names.append(self.name)
        if self.names != []:
            names.extend(self.names)
        for or_finder in self.or_finders:
            by_name = or_finder.check_by_name
            if by_name:
                names.extend(by_name)
            else:
                return [] # hack, if or_finder not check by name, ignore this check by name
        return names

################################################################################
#
CardFaceType: TypeAlias = 'CardFace'
class CardFinder2(Generic[TC], CardFinder):

    # Null: 'CardFinder2[CardFace]'

    def __init__(self,
                 trait: "CardFace.TRAITS|None"=None,
                 card_type: 'Type[TC]|None'=CardFaceType,
    ) -> None:
        from game.card.face.card_face import CardFace
        if card_type == CardFace or card_type == 'CardFace': # type: ignore
            # Hack for `CardFaceType`
            card_type = None
        card_type2 = card_type
        super().__init__(trait=trait, card_type=card_type2)

