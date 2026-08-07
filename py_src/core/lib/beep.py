from core import *
# import winsound

class Beep:

    @staticmethod
    def BeepInternal(frequency: int=1000, duration: int=100):
        # Frequency in Hertz and duration in milliseconds
        # winsound.Beep(frequency, duration)
        pass

    @staticmethod
    def NotesInternal(notes: List[Tuple[int, int]]):
        for frequency, duration in notes:
            Beep.BeepInternal(frequency, duration)

    @staticmethod
    def Success():
        notes = [
            (1000, 100),
            (1500, 100),
            (2000, 200),
            (2000, 100),
            (1500, 100),
            (1000, 400),
        ]

        Beep.NotesInternal(notes)

    @staticmethod
    def Warning():
        # winsound.MessageBeep(winsound.MB_ICONEXCLAMATION)
        notes = [
            (700, 200),
        ]

        Beep.NotesInternal(notes)

