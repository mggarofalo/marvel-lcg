from enum import Enum

class GameSignalType(Enum):
    INVALID_MOVE = "InvalidMove"
    UNDO_REQUEST = "UndoRequest"
    UNKNOWN = "Unknown"

class GameSignal(Exception):
    """Base class for non-fatal game flow signals (e.g., undo, invalid move)."""

    def __init__(self, signal_type: GameSignalType):
        self.signal_type = signal_type
        # Pass the enum value (string) to Exception's __init__ for nice printing
        super().__init__(signal_type.value)

class GameSignal_UndoRequest(GameSignal):
    def __init__(self):
        super().__init__(GameSignalType.UNDO_REQUEST)

class GameSignal_InvalidMove(GameSignal):
    def __init__(self):
        super().__init__(GameSignalType.INVALID_MOVE)

