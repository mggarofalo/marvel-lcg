from core import *

from engine.lib import Json
from engine.log import Log

class Notify:
    last_notify_id = 0
    last_show_id = 0
    descriptors: List['Notify.NotifyDescriptor'] = []

    NOTIFY_LEVEL = Literal['', 'Error']

    class NotifyDescriptor:
        def __init__(self, category: 'Log.CATEGORY|Literal[""]', level: 'Notify.NOTIFY_LEVEL', text: str) -> None:
            Notify.last_notify_id += 1
            self.id = Notify.last_notify_id
            self.level = level
            self.category = category
            self.text = text

    # Because "Undo" will not update `game_id`, we can not differentiate new scene or old one
    # So we just don't reset the `last_notify_id`
    # @staticmethod
    # def Setup():
    #     Notify.last_notify_id = 0
    #     Notify.Clean()

    @staticmethod
    def Show(category: 'Log.CATEGORY|Literal[""]', level: 'Notify.NOTIFY_LEVEL', text: str):
        Notify.descriptors.append(Notify.NotifyDescriptor(category, level, text))

    @staticmethod
    def Clean():
        Notify.descriptors = Notify.descriptors[Notify.last_show_id:]
        Notify.descriptors = Notify.descriptors[-4:]

    @staticmethod
    def Get() -> List[str]:
        Notify.last_show_id = len(Notify.descriptors)
        return [Json.Dumps(x) for x in Notify.descriptors]

    @staticmethod
    def Notify(category: 'Log.CATEGORY|Literal[""]', level: 'Notify.NOTIFY_LEVEL', text: str):
        Notify.Show(category, level, text)

    @staticmethod
    def Command(text: str):
        Notify.Show("COMMAND", "", text)

    @staticmethod
    def Game(text: str):
        Notify.Show("GAME", "", text)

    @staticmethod
    def Statistics(text: str):
        Notify.Show("STATISTICS", "", text)
        Log.Debug("STATISTICS", text)

    @staticmethod
    def Error(text: str):
        Notify.Show("", "Error", text)
        Log.Warn("NOTIFY", text)

