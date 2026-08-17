from typing import Final
from core import *
from game.card.face import *
from game.effect import *
from game.selector import *
from game.selector.selector_range import SelectorRange

CATEGORY_NAME = "SELECTOR"

class SelectorRule:

    # See effect.ts
    RULE_BASE = Literal[
        "",
        "DifferentCards",
        "DifferentType",
        "MustIncludeTraits", # Get from `traits=`
    ]

    RULE_WITH_PARAM = Literal[
        "CombinedResourceCost",
        "CombinedPrintedCost",
        "CombinedResourceIcons",
    ]

    RULE_FOR_UI = Literal[
        # Only use for villain and each minion, check effect.ts
        # Don't use this outside
        "VillainAndMinionsEngagedSamePlayer",
        "VillainAndMinionsEngagedWithYou",
    ]

    RULE_ALL = RULE_BASE|RULE_WITH_PARAM|RULE_FOR_UI

    REPEAT_RULE = Literal["Sustained", "Threat", "Health", "CanHasConfused", "Any"]

    def __init__(self,
                # Select rule
                random: bool=False,
                select_rule: 'RULE_BASE|RULE_FOR_UI'="",
                repeat_rules: List['SELECT.REPEAT_RULE']=[],
                combined_resource_cost: Tuple[int, int]|None=None,
                combined_printed_cost: Tuple[int, int]|None=None,
                combined_resource_icons: Tuple[int, int]|None=None,
                top_bottom_most: bool=False,
                target_must_include_traits: List['CardFace.TRAITS']=[]
                ) -> None:
        self.random                     : Final = random
        self.raw_select_rule            : Final = select_rule
        self.repeat_rules               : Final = repeat_rules
        self.combined_resource_cost     : Final = combined_resource_cost
        self.combined_printed_cost      : Final = combined_printed_cost
        self.combined_resource_icons    : Final = combined_resource_icons
        self.top_bottom_most            : Final = top_bottom_most

        if select_rule != "MustIncludeTraits":
            target_must_include_traits = []

        self.target_must_include_traits : Final = target_must_include_traits

        if combined_resource_cost:
            new_rule_all = "CombinedResourceCost"
            rule_param = combined_resource_cost
        elif combined_printed_cost:
            new_rule_all = "CombinedPrintedCost"
            rule_param = combined_printed_cost
        elif combined_resource_icons:
            new_rule_all = "CombinedResourceIcons"
            rule_param = combined_resource_icons
        else:
            new_rule_all = select_rule
            rule_param = (0, 0)

        self.select_rule                : Final = new_rule_all
        self.rule_param                 : Final = rule_param

    def GetRuleAndParam(self) -> Tuple['SelectorRule.RULE_ALL', Tuple[int, int]]:
        return self.select_rule, self.rule_param

    @property
    def has_combined_rules(self) -> bool:
        return bool(
            self.combined_resource_cost or \
            self.combined_printed_cost or \
            self.combined_resource_icons
        )

    ################################################################################
    #
    def Process(self, legal_faces: List['CardFace'], effect: 'Effect', selector_range: 'SelectorRange') -> List['CardFace']:
        from game.operate.faces_counter import FacesCounter
        
        if self.top_bottom_most:
            max = selector_range.GetTargetMax(effect, legal_faces)
            legal_faces = legal_faces[:max]

        if self.repeat_rules:
            from game.card.face.base import Scheme2
            from game.card.face.base import Unit2

            max_value = selector_range.GetTargetMax(effect, legal_faces)
            face_list: List[CardFace] = []
            for face in Types.RemoveDuplicates(legal_faces):
                for allow_repeat in self.repeat_rules:
                    if allow_repeat == "Threat":
                        if Scheme2.IsType(face):
                            face_list += [face] * min(face.threat, max_value)
                    elif allow_repeat == "Sustained":
                        if Unit2.IsType(face):
                            face_list += [face] * min(face.sustained, max_value)
                    elif allow_repeat == "Health":
                        if Unit2.IsType(face):
                            face_list += [face] * min(face.health, max_value)
                    elif allow_repeat == "CanHasConfused":
                        if Unit2.IsType(face):
                            if face.IsConfused():
                                pass
                            elif face.IsSteady():
                                if face.confused:
                                    face_list += [face]
                                else:
                                    face_list += [face] * 2
                            else:
                                face_list += [face]
                    elif allow_repeat == "Any":
                        face_list += [face] * max_value
            legal_faces = face_list

        if self.combined_resource_cost:
            cost = FacesCounter.GetPrintedCost(legal_faces)
            if cost < self.combined_resource_cost[0]:
                return []

        if self.combined_printed_cost:
            cost = FacesCounter.GetPrintedCost(legal_faces)
            if cost < self.combined_printed_cost[0]:
                return []

        if self.combined_resource_icons:
            icon = FacesCounter.GetPrintedResourcesIcon(legal_faces, "Auto")
            if icon < self.combined_resource_icons[0]:
                return []

        return legal_faces

    def AfterSelectTargets(self, effect: 'Effect', targets: Sequence['CardFace'], range: Tuple[int, int]) -> bool:
        from game.operate.faces_counter import FacesCounter
        from game.card.face.base import Villain
        from game.card.face.card_type import Minion
        from game.effect.effect_failure import EffectFailure
        from game.selector.factory import Select

        if not self.select_rule.startswith('VillainAndMinions'):
            if not (range[0] <= len(targets) <= range[1]):
                effect.failures.Set(None, EffectFailure.TargetNum)
                return False
        else:
            # The browser treats EncounterVillain and Leader as the villain;
            # both derive from Villain in Python. These selectors deliberately
            # skip the ordinary numeric range because they mean "the villain
            # and every matching minion", but that still requires exactly one
            # villain (MARVEL-128).
            if sum(Villain.IsType(face) for face in targets) != 1:
                effect.failures.Set(None, EffectFailure.TargetNum)
                return False

            if self.select_rule == "VillainAndMinionsEngagedWithYou":
                player = Select.GetYou(effect)
            else:
                player = next(
                    (face.GetEngagedPlayer() for face in targets
                     if Minion.IsType(face)),
                    None,
                )

            for face in targets:
                if Minion.IsType(face):
                    if player != face.GetEngagedPlayer():
                        effect.failures.Set(player, EffectFailure.EngagedDifferentPlayer)
                        return False

            # Unlike an ordinary "All" range, SamePlayer chooses one player's
            # group from a pool containing every player's minions. The client
            # therefore checks the unchosen legal targets explicitly. Mirror
            # that here: once the player is known, every minion engaged with
            # that player must be present. WithYou already has a player before
            # this check; SamePlayer deliberately refuses villain-only if any
            # minion remains in the pool, matching the client's `player_id ==
            # -1` case (MARVEL-128).
            legal_minions = [face for face in effect.context.all_legal_targets
                             if Minion.IsType(face)]
            if player is None and legal_minions:
                effect.failures.Set(None, EffectFailure.TargetNum)
                return False
            for face in legal_minions:
                if face.GetEngagedPlayer() == player and face not in targets:
                    effect.failures.Set(player, EffectFailure.TargetNum)
                    return False

        # "Choose up to 3 *different* cards". Two copies of one title are one
        # card for the purpose of card abilities, so the second copy is not an
        # available second choice. This is the engine-side counterpart of
        # `check_select_rule` in `public/js/marvel/effect.ts`, which compares
        # `Cards.getCard(id).name` -- the printed title, not the object -- and
        # until MARVEL-118 was the only implementation anywhere, so a player who
        # was not a browser could pick a pair the browser refused.
        if self.select_rule == "DifferentCards":
            if FacesCounter.GetDifferentCardsCount(targets) != len(targets):
                effect.failures.Set(effect.initiator, EffectFailure.SameCardChosen)
                return False

        # "Exhaust an AVENGER and a GUARDIAN" -- the *chosen pair* has to cover
        # both traits. `EffectChecker.UpdateLegalTargets` asks whether the pool
        # could, so an impossible cost is never offered; nothing asked whether
        # the selection did, and `SelectorFactory.Alliance` passes a
        # `CardFinder(traits=...)` whose traits are an **OR**. So the pool for
        # every Alliance card is "friends who are AVENGER *or* GUARDIAN", two
        # AVENGERs satisfied the finder and the feasibility check alike, and a
        # player who was not a browser could pay the cost with them (MARVEL-127).
        #
        # Traits are struck off the required list as they are found, matching
        # `check_select_rule` in `public/js/marvel/effect.ts` -- so one card
        # carrying both required traits covers both, on either side.
        if self.select_rule == "MustIncludeTraits":
            left_must_include_traits = list(self.target_must_include_traits)
            for face in targets:
                if not left_must_include_traits:
                    break
                found = face.FindHasTrait(*left_must_include_traits)
                left_must_include_traits = [x for x in left_must_include_traits
                                            if x not in found]
            if left_must_include_traits:
                effect.failures.Set(effect.initiator, EffectFailure.MissingRequiredTrait)
                return False

        # "Choose two cards of different types". The same shape as
        # `DifferentCards` above and the same gap it had: the pool was checked
        # and the selection was not. Its one user (`45017`) hand-rolled the
        # pairing in a `check_again_fn`, which is why nothing was visibly broken
        # -- and a rule whose correctness depends on every caller
        # re-implementing it is not a rule. MARVEL-127.
        if self.select_rule == "DifferentType":
            if FacesCounter.GetDifferentTypesCount(targets) != len(targets):
                effect.failures.Set(effect.initiator, EffectFailure.SameTypeChosen)
                return False

        if self.combined_resource_cost:
            cost = FacesCounter.GetPrintedCost(targets)
            if cost < self.combined_resource_cost[0]:
                return False
            if cost > self.combined_resource_cost[1]:
                return False

        if self.combined_printed_cost:
            cost = FacesCounter.GetPrintedCost(targets)
            if cost < self.combined_printed_cost[0]:
                return False
            if cost > self.combined_printed_cost[1]:
                return False

        if self.combined_resource_icons:
            icon = FacesCounter.GetPrintedResourcesIcon(targets, "Auto")
            if icon < self.combined_resource_icons[0]:
                return False
            if icon > self.combined_resource_icons[1]:
                return False

        return True
