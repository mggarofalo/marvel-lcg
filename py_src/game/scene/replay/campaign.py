from core import *
from game.game_run.game_challenge import GameChallenge

@dataclass
class CampaignDescriptor:
    version: str = field(default="")
    name: str = field(default="")
    villain: List[str] = field(default_factory=lambda: [])
    expert: bool = field(default=False)
    # campaign: bool = field(default=False)
    challenges: List['GameChallenge.CHALLENGE'] = field(default_factory=lambda: [])
    # custom_script: str = field(default="")
    schemes: List[str] = field(default_factory=lambda: [])
    set_aside: List[str] = field(default_factory=lambda: [])
    encounters: List[str] = field(default_factory=lambda: [])
    encounter_sets: List[str] = field(default_factory=lambda: []) # "standard", "expert"
    modular_sets: List[str] = field(default_factory=lambda: []) # Only for json
    campaign_log: Dict[str, str] = field(default_factory=lambda: {})

    def UpdateVersion(self):
        pass

    def SetChallenge(self, *challenges: 'GameChallenge.CHALLENGE'):
        self.challenges = list(sorted(set(challenges)))

