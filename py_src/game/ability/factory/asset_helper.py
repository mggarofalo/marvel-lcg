from . import *

################################################################################
# Asset
# Only work for attached card
# filter: "44011", "16016"
# "22035"
def GiveKeywordToAttachWhenApplyThisInternal(
    # filter: Callable[['Effect', 'CardFace'], int]|None=None,
    card_finder: CardFinder|Literal["Character", "You"]|Type['TC']|None=None,
    *,
    ignore_flip: bool,
    # condition: Callable[['Effect'], bool]|None=None,
    # "Identity", "Enemy", "Villain", "Ally", "Minion", "Hero", "AlterEgo", "Friend", "Scheme", "MainScheme",
    name: str|None=None,
    get_new_value: Callable[['Effect', 'TC', List['CardFace']], int]|None=None, # condition, for each
    expert_mode_only: Literal[True]|None=None,
    #
    trait: "CardFace.TRAITS|None"=None,
    attack: int|None=None,
    defense: int|None=None,
    thwart: int|None=None,
    scheme: int|None=None,
    retaliate: int|None=None,
    # ally_limit: int|None=None,
    hand_size: int|None=None,
    stalwart: int|None=None,
    health: Callable[[Effect], int]|int|Literal["3*", "6*"]|None=None,
    considered_at_least_hp: int|None=None,
    recover: int|None=None,
    guard: int|None=None,
    patrol: int|None=None,
    steady: int|None=None,
    treat_as_blank: bool|None=None,
    target_threat: int|None=None,
    control_additional_restricted: 'CardFinder|None'=None,
    attack_consequential_damage: int|None=None,
    thwart_consequential_damage: int|None=None,
    assault: int|None=None,
    have_res_icon: "Resources.RBYG|None"=None,
    apply: Callable[['Effect','CardFace', int], None]|None=None,
    ex_change_on_event: EventType=OnEvent.NONE,
    ) -> 'Ability':
    from game.ability.factory import AbilityFactory
    from game.operate.worlds import Worlds

    ex_change_on_event = Condition.AddEvent(ex_change_on_event, OnEvent.AssetEffect())
    # ex_change_on_event = Condition.AddEvent(ex_change_on_event, OnEvent.CardInPlay("This"))

    def gain_keyword(effect: 'Effect', face: 'CardFace',  diff: int) -> None:

        face.Gain(effect, diff,
            trait=trait,
            attack=attack,
            defense=defense,
            thwart=thwart,
            scheme=scheme,
            retaliate=retaliate,
            # ally_limit=ally_limit,
            hand_size=hand_size,
            stalwart=stalwart,
            health=health,
            considered_at_least_hp=considered_at_least_hp,
            recover=recover,
            guard=guard,
            patrol=patrol,
            steady=steady,
            treat_as_blank=treat_as_blank,
            target_threat=target_threat,
            attack_consequential_damage=attack_consequential_damage,
            thwart_consequential_damage=thwart_consequential_damage,
            assault=assault,
            have_res_icon=have_res_icon,
            control_additional_restricted=control_additional_restricted,
            apply=apply,
        )

    def when_unit_apply_this_asset(filter: Callable[['Effect', 'CardFace'], int],
                                    apply: Callable[['Effect','CardFace', int], None],) -> 'Ability':
        from game.card.face.attribute.can_attach import CanAttach

        def condition(effect: 'Effect', message: 'Message2|None') -> bool:
            this = effect.this.CastTo(CanAttach)

            def check():
                if isinstance(message, Message.AfterCardGainUpgradeAbility|Message.AfterCardLostUpgradeAbility):
                    if this != message.upgrade:
                        return False
                    # TODO: Check this
                    # if not Condition.ThisAttachedTo(effect, message.trigger, False):
                    if not Condition.ThisAttachedTo(effect, message.trigger):
                        return False
                    return True
                # if isinstance(message, Message.AfterCardLeavePlay|Message.WhenCardFlip|Message.AfterCardEnterPlay):
                #     if not Condition.ThisAttachedTo(effect, message.trigger, False):
                #         return False
                #     return True
                return True

            if check():
                return True
            else:
                return False

        def check_fn(effect: 'Effect', last_data: int) -> int:
            this = effect.this.CastTo(CanAttach)
            Unused(this)

            if not this.bind_face:
                return 0

            new_data: int = last_data
            message = effect.bind_message

            if isinstance(message, Message.AfterCardGainUpgradeAbility|Message.AfterCardLostUpgradeAbility):
                unit = message.trigger
                if isinstance(message, Message.AfterCardLostUpgradeAbility):
                    new_data = 0
                else:
                    new_data = filter(effect, unit)
            elif isinstance(message, Message.WhenCardTreatAsIfBlank):
                unit = this.GetBindFace()
                new_data = filter(effect, unit)
            else:
                unit = this.GetBindFace()
                new_data = filter(effect, unit)

            # Gain trait
            if unit.card.state.is_flipping and this.GetBindFace() == unit:
                if ignore_flip:
                    return last_data
                else:
                    return 0

            new_data = int(new_data)
            # if last_data != new_data:
            #     pass
            last_data = new_data
            return new_data

        def process_fn(effect: 'Effect', diff: int, curr: int) -> None:
            this = effect.this.CastTo(CanAttach)
            Unused(this)

            message = effect.bind_message
            if type(message) is Message.AfterCardGainUpgradeAbility:
                # We comment out this for "43036"
                # assert diff >= 0
                unit = message.trigger
            elif type(message) is Message.AfterCardLostUpgradeAbility:
                assert diff <= 0
                unit = message.trigger
            else:
                unit = this.bind_face

            if unit and unit.IsInPlay() and not unit.card.state.is_discarding:
                apply(effect, unit, diff)
                Message.AfterCardApply_Text([unit], effect, diff)

        return AbilityFactory.WhileValid(
            ex_change_on_event,
            condition,
            check_fn,
            process_fn
        )

    from game.card.face.base import Unit2

    if card_finder == None:
        new_card_finder = None
    elif isinstance(card_finder, CardFinder):
        new_card_finder = card_finder
    elif not isinstance(card_finder, str):
        new_card_finder = CardFinder(card_type=card_finder)
    elif card_finder == "Character":
        new_card_finder = CardFinder(card_type=Unit2)
    elif card_finder == "You":
        new_card_finder = None

    def new_filter(effect: 'Effect', face: 'CardFace') -> int:
        # this = effect.this
        # face = this.GetBindFace()

        if name != None:
            if not face.IsName(name):
                return 0

        from game.card.face.attribute.can_attach import CanAttach
        if CanAttach.IsType(effect.this) and \
            effect.this.is_refers_to_the_villain_refers_to_attached:
            return 1

        if new_card_finder:
            if not new_card_finder.Check(face):
                return 0

        if expert_mode_only:
            if not Worlds.IsExpert(effect):
                return 0

        if get_new_value:
            faces: List['CardFace'] = []
            value = get_new_value(effect, Cast(Any, face), faces)
            for face in faces:
                effect.this.card.ui.SetTempEffectBy(effect, face)
            return value
        else:
            return 1

    return when_unit_apply_this_asset(
        new_filter,
        gain_keyword,
    )

