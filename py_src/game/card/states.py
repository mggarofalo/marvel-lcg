class CardIsStates:

    def __init__(self) -> None:
        self.is_flipping        = False
        self.is_leaving_play    = False
        self.is_discarding      = False
        self.is_defeating       = False
        self.is_revealing       = False
        self.is_swapping_begin  = False
        self.is_swapping_end    = False
        self.is_setting_as      = False
        self.is_advancing       = False # villain
        self.is_face_up         = True
        self.is_ready           = True
        self.is_attached        = False

class CardCanStates:

    def __init__(self) -> None:
        self.is_like_in_hand: bool|None = None
        self.is_can_ready: bool|None = None

    def Reset(self):
        self.is_like_in_hand = None
        self.is_can_ready = None

