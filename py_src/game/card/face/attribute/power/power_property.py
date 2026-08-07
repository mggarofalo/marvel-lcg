from core import *

@dataclass
class PowerProperty:
    is_basic_power: bool = field(default=False)
    is_divided: bool = field(default=False) # "13001c"
    divided_evenly: bool = field(default=False) # "29033"
    # base_value: int = field(default=0)
    additional_value: int = field(default=0) # With +X ATK/SCH

