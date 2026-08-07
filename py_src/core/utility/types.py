from core import *

T = TypeVar("T")

class Types:

    @staticmethod
    def Rotate(item_list: List[T], n: int) -> List[T]:
        return item_list[n:] + item_list[:n]

    @staticmethod
    def UnionTypeExtract(input_type: Type[Any]) -> List[Type[Any]]:
        if hasattr(input_type, '__args__'):
            return list(get_args(input_type))
        else:
            return [input_type]

    @staticmethod
    def LiteralToList(literal: Any) -> List[Any]:
        if hasattr(literal, '__args__'):
            return list(get_args(literal))
        else:
            return []

    @staticmethod
    def LiteralToDict(literal: Any) -> Dict[str, int]:
        if hasattr(literal, '__args__'):
            return {char: 0 for char in list(get_args(literal))}
        else:
            return {}

    @staticmethod
    def RemoveDuplicates(input_list: List[T]) -> List[T]:
        seen: Set[Any] = set()
        return [x for x in input_list if not (x in seen or seen.add(x))]

    @staticmethod
    # "1, 2, 3" -> ["1", "2", "3"]
    def StrListToList(str_list: str) -> List[str]:
        if str_list == "":
            return []
        return str_list.split(", ")

    @staticmethod
    # ["1", "2", "3"] -> "1, 2, 3"
    def ListToStrList(int_list: List[Any]) -> str:
        return ", ".join(int_list)

    @staticmethod
    def DictChecksum(obj: Dict[Any, Any]) -> str:
        import hashlib
        from engine.lib import Json
        text = Json.Dumps(obj, sort_keys=True)
        checksum = hashlib.sha256(text.encode('utf-8')).hexdigest()
        return checksum

