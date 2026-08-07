from core import *

T = TypeVar("T")

class Math:
    INT_INF = 65535

    @staticmethod
    def DivideEvenly(num: int, lst: List[Any]):
        length = len(lst)

        if length == 0:
            return 0, num  # If the list is empty, all goes to the rest

        # Calculate the value for each element in the list
        value_per_element = num // length
        remainder = num % length

        return value_per_element, remainder

        # Example usage:
        print(DivideEvenly(3, ['a','b','c']))   # (1, 0)
        print(DivideEvenly(6, ['a','b','c']))   # (2, 0)
        print(DivideEvenly(9, ['a','b','c']))   # (3, 0)
        print(DivideEvenly(4, ['a','b']))       # (2, 0)

        print(DivideEvenly(1, ['a','b','c']))   # (0, 1)
        print(DivideEvenly(4, ['a','b','c']))   # (1, 1)
        print(DivideEvenly(5, ['a','b','c']))   # (1, 2)
        print(DivideEvenly(3, ['a','b']))       # (1, 1)

    @staticmethod
    def AllMax(item_list: Sequence[T], key: Callable[[T], int]) -> List[T]:
        if item_list == []:
            return []
        item = max(item_list, key=key)
        the_key = key(item)
        return [x for x in item_list if key(x) == the_key]

    @staticmethod
    def AllMin(item_list: Sequence[T], key: Callable[[T], int]) -> List[T]:
        if item_list == []:
            return []
        item = min(item_list, key=key)
        the_key = key(item)
        return [x for x in item_list if key(x) == the_key]

    @staticmethod
    def MinMax(x: int, num_min: int, num_max: int) -> int:
        if x > num_max:
            x = num_max
        if x < num_min:
            x = num_min
        return x

    @staticmethod
    def MaxAndIndex(numbers: Sequence[int]) -> Tuple[int, int]:
        max_number = max(numbers)
        max_index = numbers.index(max_number)

        return max_number, max_index

