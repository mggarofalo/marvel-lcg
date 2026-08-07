from core import *
from build import Build

T = TypeVar("T")

def Cast(t: Type[T], v: Any) -> T:
    from game.selector.factory import SELECT
    from game.selector.selector_rule import SelectorRule
    from game.selector.selector_target import SelectorTarget
    from game.selector.selector_where import SelectorWhere
    from game.card.card_finder import CardFinderHelper
    Unused(SELECT, SelectorRule, SelectorTarget, CardFinderHelper, SelectorWhere)
    return cast(t, v)

    def unpack_aliases(type_alias: Any, globalns: dict[str, Any]|None = None, localns: dict[str, Any]|None = None) -> List[Any]:
        from typing import _eval_type # type: ignore
        args: Tuple[Any, ...] = get_args(type_alias)
        values: List[Any] = []
        for arg in args:
            if isinstance(arg, ForwardRef):
                # Resolve the forward reference
                resolved_arg: Any = _eval_type(arg, globalns, localns)
                values.extend(unpack_aliases(resolved_arg, globalns, localns))
            elif isinstance(arg, str):
                # Evaluate the string to get the actual alias
                # alias: Any = eval(arg)
                # values.extend(unpack_aliases(alias, globalns, localns))
                values.append(arg)
            else:
                values += get_args(arg)
                # values.append(arg)
        return values

    if get_origin(t) == Union:
        assert v in unpack_aliases(t, globals(), locals())
        # assert v in [value for arg in get_args(t) for value in get_args(arg)]
    elif get_origin(t) == Literal:
        assert v in list(get_args(t)), f"{v=} {t=}"
    elif get_origin(t) == list:
        # We didn't check list
        pass
    elif t == Any:
        pass
    else:
        from game.card.face.card_face import CardFace
        if isinstance(v, CardFace):
            assert isinstance(v, t) or v.IsConsiderAs(card_type=t) # type: ignore
        else:
            assert isinstance(v, t), f"{v=} {t=}"
    return cast(t, v)

if Build.release:
    Cast = cast # type: ignore

