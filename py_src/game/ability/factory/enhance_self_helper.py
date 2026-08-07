from . import *

def ThisGainKeywordInternal(change_on_event: EventType,
                            # condition_fn: ConditionType[TM],
                            check_fn: Callable[['Effect', List['CardFace']], int|Tuple[int, bool]],
                            process_fn: Callable[['Effect', 'CardFace', int, int], None],
    ) -> 'Ability':
        from game.card.face.base import Unit2
        from game.card.face.base import Scheme2
        from game.card.face.card_type import Support
        from game.card.face.card_type import Upgrade
        from game.card.face.card_type import Attachment

        def process(effect: 'Effect', diff: int, curr: int) -> None:
            unit = effect.this
            assert Unit2.IsType(unit) or Scheme2.IsType(unit) or Support.IsType(unit) or Upgrade.IsType(unit) or Attachment.IsType(unit)

            process_fn(effect, unit, diff, curr)

            Message.AfterCardApply_Text([unit], effect, diff)

        def check(effect: 'Effect', last_value: int):
            faces: List['CardFace'] = []
            value = check_fn(effect, faces)
            for face in faces:
                effect.this.card.ui.SetTempEffectBy(effect, face)
            return value

        from game.ability.factory import AbilityFactory
        return AbilityFactory.WhileValid(
            change_on_event,
            lambda effect, message: True,
            check,
            process,
        )

