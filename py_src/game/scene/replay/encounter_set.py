from core import *

@dataclass
class EncounterSetDescriptor:
    version: str
    name: str
    encounters: List[str]

