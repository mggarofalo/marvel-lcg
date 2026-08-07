from typing import Final
from core import *
from game.world import *
from game.object import *

class Object:

    def __init__(self, category: 'ObjectManager.OBJECT_CATEGORY', world: 'World') -> None:
        object_id = world.object_manager.AddObject(category, self)

        self.object_id      : Final = object_id
        self.object_category: Final = category

        self.world = world

