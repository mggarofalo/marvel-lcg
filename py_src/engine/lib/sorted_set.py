from core import *
import bisect

T = TypeVar('T')

class SortedSet(Generic[T]):
    def __init__(self, elements: Iterable[T]|None = None) -> None:
        self._data: List[T] = []
        if elements is not None:
            for element in elements:
                self.add(element)

    def add(self, element: T) -> None:
        if element not in self._data:
            bisect.insort(self._data, element) # type: ignore

    def remove(self, element: T) -> None:
        if element in self._data:
            self._data.remove(element)

    def discard(self, value: T) -> None:
        if value in self._data:
            self._data.remove(value)

    def clear(self) -> None:
        self._data.clear()

    def __contains__(self, element: T) -> bool:
        return element in self._data

    def __iter__(self):
        return iter(self._data)

    def __len__(self) -> int:
        return len(self._data)

    def __repr__(self) -> str:
        return f"SortedSet({self._data})"

    def __eq__(self, other: object) -> bool:
        if isinstance(other, SortedSet):
            return self._data == other._data # type: ignore
        return False

    def __ne__(self, other: object) -> bool:
        return not self.__eq__(other)

    def __and__(self, other: 'SortedSet[T]') -> 'SortedSet[T]':
        return SortedSet(value for value in self._data if value in other._data)

    def __or__(self, other: 'SortedSet[T]') -> 'SortedSet[T]':
        return SortedSet(self._data + [value for value in other._data if value not in self._data])

    def __sub__(self, other: 'SortedSet[T]') -> 'SortedSet[T]':
        return SortedSet(value for value in self._data if value not in other._data)

    def __xor__(self, other: 'SortedSet[T]') -> 'SortedSet[T]':
        return SortedSet(value for value in self._data + other._data if (value in self._data) ^ (value in other._data))

    def union(self, *others: Iterable[T]) -> 'SortedSet[T]':
        result = SortedSet(self._data)
        for other in others:
            result.update(other)
        return result

    def intersection(self, *others: Iterable[T]) -> 'SortedSet[T]':
        result = SortedSet(self._data)
        for other in others:
            result = result & SortedSet(other)
        return result

    def difference(self, *others: Iterable[T]) -> 'SortedSet[T]':
        result = SortedSet(self._data)
        for other in others:
            result = result - SortedSet(other)
        return result

    def symmetric_difference(self, other: Iterable[T]) -> 'SortedSet[T]':
        return self ^ SortedSet(other)

    def update(self, *others: Iterable[T]) -> None:
        for other in others:
            for value in other:
                self.add(value)

    def intersection_update(self, *others: Iterable[T]) -> None:
        result = self.intersection(*others)
        self._data = result._data

    def difference_update(self, *others: Iterable[T]) -> None:
        result = self.difference(*others)
        self._data = result._data

    def symmetric_difference_update(self, other: Iterable[T]) -> None:
        result = self.symmetric_difference(other)
        self._data = result._data

if __name__ == "__main__":
    my_set = SortedSet([3, 1, 2])
    print(my_set)  # Output: SortedSet([1, 2, 3])
    print(2 in my_set)  # Output: True
    my_set.add(4)
    print(my_set)  # Output: SortedSet([1, 2, 3, 4])
    my_set.remove(2)
    print(my_set)  # Output: SortedSet([1, 3, 4])
    print(len(my_set))  # Output: 3

