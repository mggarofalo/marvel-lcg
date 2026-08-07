from . import *

class Stores:

    @staticmethod
    def SetStr(name: str, value: str, by_effect: 'Effect'):
        world = by_effect.world
        world.store.SetStr(name, value)

    @staticmethod
    def GetStr(name: str, by_effect: 'Effect') -> str:
        world = by_effect.world
        return world.store.GetStr(name)

    @staticmethod
    def HasKey(name: str, by_effect: 'Effect') -> bool:
        world = by_effect.world
        return world.store.HasKey(name)

    @staticmethod
    def SetFace(name: str, value: 'CardFace', by_effect: 'Effect'):
        world = by_effect.world
        world.store.SetFace(name, value)

    @staticmethod
    def GetFace(name: str, by_effect: 'Effect') -> 'CardFace':
        world = by_effect.world
        return world.store.GetFace(name)

    @staticmethod
    def AddFace(name: str, value: 'CardFace', by_effect: 'Effect'):
        world = by_effect.world
        world.store.AddFace(name, value)

    @staticmethod
    def GetFaces(name: str, by_effect: 'Effect') -> List['CardFace']:
        world = by_effect.world
        return world.store.GetFaces(name)

