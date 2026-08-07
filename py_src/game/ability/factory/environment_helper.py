from . import *

# TODO: Add card type as param
# No need to be attached
# `filter.effect` used in "21015" "17001a" "17009" "04016"
def GiveKeywordToInPlayWhenApplyThisInternal(
    card_finder: CardTypeMin=None,
    *,
    ignore_flip: bool=False,
    control_by: Literal["You", "EngagedPlayer"]|None=None,
    get_new_value: Callable[['Effect', 'CardFace'], int|List[str]]|None=None,
    condition: Callable[['Effect'], bool]|None=None,
    if_each_of_your_characters_has_trait: "CardFace.TRAITS|None"=None,
    if_each_of_your_allies_has_trait: "CardFace.TRAITS|None"=None,
    works_for_all_cards: bool=False,
    works_in_the_victory_display: bool=False, # 40006
    expert_mode_only: Literal[True]|None=None,
    #
    trait: List["CardFace.TRAITS"]|"CardFace.TRAITS|None"|Literal[1]=None,
    teamwork: List["CardFace.TRAITS"]|"CardFace.TRAITS|None"|Literal[1]=None, # 1 means get the list result from `get_new_value`
    attack: int|None=None,
    defense: int|None=None,
    thwart: int|None=None,
    scheme: int|None=None,
    retaliate: int|None=None,
    hand_size: int|None=None,
    stalwart: int|None=None,
    health: int|None=None,
    recover: int|None=None,
    guard: int|None=None,
    steady: int|None=None,
    treat_as_blank: bool|None=None,
    quickstrike :int|None=None,
    attack_consequential_damage: int|None=None,
    thwart_consequential_damage: int|None=None,
    patrol: int|None=None,
    surge: int|None=None,
    incite: int|None=None,
    acceleration_icon: int|None=None,
    hazard: int|None=None,
    toughness: int|None=None,
    not_include_this: bool=False,
    ally_limit: int|None=None,
    flag: 'PlayerFlag.FLAG|None'=None,
    peril: int|None=None,
    temporary: int|None=None,
    permanent: int|None=None,
    villainous: int|None=None,
    target_threat: int|None=None,
    # additional_cost_to_ready: 'CostFunc.Base'|None=None,
    additional_cost_for_basic_attack: 'CostFunc.Base|None'=None,
    additional_cost_for_basic_thwart: 'CostFunc.Base|None'=None,
    additional_cost_for_basic_defend: 'CostFunc.Base|None'=None,
    base_sch: int|None=None,
    base_atk: int|None=None,
    base_health: int|None=None,
    abilities: List[Ability]|None=None,
    buffs: List[Type['Buff']]|None=None,
    apply: Callable[['Effect','CardFace', int], None]|None=None,
    incite_only: bool=False,
    change_on_event: EventType,
    ) -> 'Ability':
    from game.operate.faces import Faces
    from game.card.card_finder import CardFinder, CardFinder2
    from game.operate.worlds import Worlds

    # check_when_events.append(
    #     Message.WhenCardEnterPlay, Message.WhenCardLeavePlay, Message.AfterCardEnterPlay, Message.AfterCardLeavePlay, Message.WhenVillainWouldAdvance
    # )

    if works_in_the_victory_display:
        change_on_event = Condition.AddEvent(change_on_event, OnEvent.Victory("This"))

    if isinstance(card_finder, CardFinder):
        change_on_event = Condition.AddEvent(change_on_event, OnEvent.EnterPlayWhen(card_finder))
    elif isinstance(card_finder, type):
        change_on_event = Condition.AddEvent(change_on_event, OnEvent.EnterPlayWhen(card_finder))
    else:
        change_on_event = Condition.AddEvent(change_on_event, OnEvent.EnterPlayWhen(card_finder))

    if control_by:
        change_on_event = Condition.AddEvent(change_on_event, OnEvent.PlayAreaCard(card_finder))

    def new_condition(effect: 'Effect') -> bool:
        if works_in_the_victory_display and not effect.this.card.area.flags.is_victory_display:
            return False
        if condition != None:
            if not condition(effect):
                return False
        if if_each_of_your_characters_has_trait:
            if not Faces.EachOfAre(effect.GetInitiator().GetControlCharacters(), CardFinder2(if_each_of_your_characters_has_trait)):
                return False
        if if_each_of_your_allies_has_trait:
            if not Faces.EachOfAre(effect.GetInitiator().GetControlAllies(), CardFinder2(if_each_of_your_allies_has_trait)):
                return False
        if expert_mode_only:
            if not Worlds.IsExpert(effect):
                return False
        return True

    from game.ability.factory.environment_helper2 import StoreValue, StoreValueDiff
    def gain_keyword(effect: 'Effect', unit: 'CardFace', diff: 'StoreValueDiff', is_apply: bool) -> None:
        if not_include_this and effect.this == unit:
            return

        if trait == 1 or teamwork == 1:
            if is_apply == True:
                the_list = diff.lst_add
                diff_val = 1
            else:
                the_list = diff.lst_del
                diff_val = -1

            unit.Gain(
                effect=effect,
                diff=diff_val,
                trait=the_list if trait else None, # type: ignore
                teamwork=the_list if teamwork else None, # type: ignore
            )
        else:
            diff_val = diff.diff
            unit.Gain(
                effect=effect,
                diff=diff_val,
                trait=trait,
                teamwork=teamwork,
                thwart=thwart,
                attack=attack,
                defense=defense,
                scheme=scheme,
                retaliate=retaliate,
                hand_size=hand_size,
                stalwart=stalwart,
                health=health,
                recover=recover,
                guard=guard,
                steady=steady,
                treat_as_blank=treat_as_blank,
                quickstrike=quickstrike,
                attack_consequential_damage=attack_consequential_damage,
                thwart_consequential_damage=thwart_consequential_damage,
                patrol=patrol,
                surge=surge,
                incite=incite,
                acceleration_icon=acceleration_icon,
                hazard=hazard,
                toughness=toughness,
                ally_limit=ally_limit,
                flag=flag,
                peril=peril,
                temporary=temporary,
                permanent=permanent,
                villainous=villainous,
                target_threat=target_threat,
                additional_cost_for_basic_attack=additional_cost_for_basic_attack,
                additional_cost_for_basic_thwart=additional_cost_for_basic_thwart,
                additional_cost_for_basic_defend=additional_cost_for_basic_defend,
                base_sch=base_sch,
                base_atk=base_atk,
                base_health=base_health,
                abilities=abilities,
                buffs=buffs,
                apply=apply,
            )

        Message.AfterCardApply_Text([unit], effect, diff_val, no_present=True)

    def new_get_value(effect: 'Effect', face: 'CardFace') -> 'StoreValue':
        from game.card.face.card_type import Minion
        from game.ability.condition import Condition

        if card_finder == None:
            pass
        if not Condition.CheckWhichCard(card_finder, face, effect):
            return StoreValue(0, [])

        if control_by == "You":
            if effect.this.IsInPlay() and Select.GetYou(effect) != face.GetControlBy():
                return StoreValue(0, [])
        elif control_by == "EngagedPlayer":
            if effect.this.CastTo(Minion).engaged_player != face.GetControlBy():
                return StoreValue(0, [])

        if get_new_value:
            ret_val = get_new_value(effect, face)
            if isinstance(ret_val, int):
                return StoreValue(ret_val, [])
            else:
                return StoreValue(0, ret_val)

        return StoreValue(1, [])

    from game.ability.factory.environment_helper2 import WhenFaceApplyThisInternal
    return WhenFaceApplyThisInternal(
        card_finder,
        new_condition,
        new_get_value,
        gain_keyword,
        ignore_flip,
        change_on_event,
        works_for_all_cards=works_for_all_cards,
        works_in_the_victory_display=works_in_the_victory_display,
        incite_only=incite_only,
        abilities=abilities,
        buffs=buffs,
    )

