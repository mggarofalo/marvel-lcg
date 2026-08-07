from core import *
from build import Build

class InstanceTracker:

    instance_list: Dict[str, List[Any]] = {}
    instance_creation_locations: Dict[str, Dict[str, int]] = {}

    class Meta(type):
        pass

    class MetaDebug(Meta):
        def __init__(cls, name: str, bases: Tuple[type], dct: Dict[str, type]) -> None:
            super().__init__(name, bases, dct)
            InstanceTracker.instance_list[name] = []
            InstanceTracker.instance_creation_locations[name] = {}

        def __call__(cls, *args: Any, **kwargs: Any) -> Any:
            location = GetCallStack(2)

            if location in InstanceTracker.instance_creation_locations[cls.__name__]:
                InstanceTracker.instance_creation_locations[cls.__name__][location] += 1
            else:
                InstanceTracker.instance_creation_locations[cls.__name__][location] = 1

            instance = super().__call__(*args, **kwargs)
            InstanceTracker.instance_list[cls.__name__].append(instance)
            return instance

    if not Build.release:
        Meta = MetaDebug

    @classmethod
    def ResetStats(cls) -> None:
        for key in InstanceTracker.instance_list.keys():
            InstanceTracker.instance_list[key] = []
            InstanceTracker.instance_creation_locations[key] = {}

    @classmethod
    def PrintStats(cls) -> str:
        import json
        # {InstanceTracker.instance_creation_locations=}
        instance_count: Dict[str, int] = {key: len(value) for key, value in InstanceTracker.instance_list.items()}
        locations = json.dumps(InstanceTracker.instance_creation_locations, indent=4)
        text = f"""{instance_count=}
locations: {locations}
"""
        return text

# Example usage
if __name__ == "__main__":
    class MyClass(metaclass=InstanceTracker.Meta):
        @Tracker.count_calls
        def my_method(self) -> None:
            pass

    class MyClass2(metaclass=InstanceTracker.Meta):
        @Tracker.count_calls
        def my_method_2(self) -> None:
            pass

    obj1 = MyClass()
    obj2 = MyClass()
    obj3 = MyClass2()

    obj1.my_method()
    obj1.my_method()
    obj3.my_method_2()

    print(Tracker.PrintStats())

    # Reset stats
    InstanceTracker.ResetStats()
    Tracker.ResetStats()

    print(InstanceTracker.PrintStats())
    print(Tracker.ResetStats())

