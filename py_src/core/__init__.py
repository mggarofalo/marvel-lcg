def Unused(*_: object):
    pass

from typing import Any, Callable, Dict, List, Sequence, Type, Set, Tuple
Unused(Any)
Unused(Dict, List, Set, Tuple, Sequence)
Unused(Type[Callable[[], Any]])

# from typing import TypeAlias
# Unused(TypeAlias)

# from typing import Final
# Unused(Final)
# from typing_extensions import Final
# Unused(Final)

from typing import TypeVar
Unused(TypeVar)

from typing import Literal
Unused(Literal)

from typing import Union
Unused(Union)

from typing import TypeGuard
Unused(TypeGuard)

from typing_extensions import override
Unused(override)

from copy import copy
Unused(copy)

from typing import final
Unused(final)

from dataclasses import dataclass, field
from dataclasses import asdict
from dataclasses import fields, is_dataclass
Unused(dataclass, field)
Unused(asdict)
Unused(fields, is_dataclass)

from typing import Generic, Iterable
Unused(Generic, Iterable)

from typing import Awaitable
Unused(Awaitable)

from typing import ForwardRef, cast, get_args, get_origin
Unused(ForwardRef, cast, get_args, get_origin)

from typing import TypedDict
Unused(TypedDict)

from typing_extensions import Unpack
Unused(Unpack)

from typing import NoReturn
Unused(NoReturn)

from typing import overload
Unused(overload)
from typing_extensions import deprecated
Unused(deprecated)

# from typing_extensions import NotRequired
# Unused(NotRequired)

from core.utility.types import Types
Unused(Types)
# Rotate, UnionTypeExtract, LiteralToList, LiteralToDict, RemoveDuplicates, StrListToList, ListToStrList
# Unused(Rotate, UnionTypeExtract, LiteralToList, LiteralToDict, RemoveDuplicates, StrListToList, ListToStrList)

from core.utility.system import System
Unused(System)

from core.utility.cast import Cast
Unused(Cast)

from core.utility.utility import Unquote
Unused(Unquote)

from core.utility.debug import Debug
Unused(Debug)

from core.utility.func import GetFuncLines, GetFuncName, GetCallStack, IsAsyncFunction, IsNonAsyncFunction
Unused(GetFuncLines, GetFuncName, GetCallStack, IsAsyncFunction, IsNonAsyncFunction)

from core.meta_class import Tracker
from core.meta_class.class_name import ClassNameMeta
Unused(Tracker)
Unused(ClassNameMeta)

from core.lib.math import Math
Unused(Math)

import collections
Unused(collections)

