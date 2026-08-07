from game.card.face import *
from game.ability import *
from game.message import *
from game.ability.condition import Condition

class Condition2:

    @staticmethod
    def ThisIsTrigger(effect: 'Effect', message: 'TriggerMessage'):
        return Condition.CheckWhichCard("This", message.trigger, effect)

