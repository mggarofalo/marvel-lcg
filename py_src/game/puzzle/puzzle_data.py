from core import *

@dataclass
class PuzzleData:
    version: str
    author: str
    cover: str
    comment: str
    seed: int
    villain: List[str]
    schemes: List[str]
    encounter_deck: List[str]
    encounter_discard_pile: List[str]
    set_aside: List[str]
    expert: bool
    @dataclass
    class PlayerData:
        identities: List[str]
        hand_cards: List[str]
        player_deck: List[str]
        player_discard_pile: List[str]
        set_aside: List[str]
    players: List[PlayerData]
    puzzle_command: List[str]

