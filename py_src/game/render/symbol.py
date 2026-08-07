from core import *
from colorama import Fore, Back, Style
import re

@dataclass
class Symbol:
    health = f'{Fore.LIGHTRED_EX}♡{Fore.RESET}'
    threat = f'{Fore.YELLOW}⚠{Fore.RESET}'

    number = f'{Back.LIGHTRED_EX}'
    func_name = f'{Back.LIGHTBLUE_EX}'
    reset = f'{Style.RESET_ALL}'

    # * Unique
    # ♙ "per player"
    # Card
    # ╚ - * └ ┕ ┖ ┗ ⌞ ⌎ ⌦ ⌂ ⎿ ⎆ ⍟
    # 🂿 🃳 🂼 🃴 ⍐ ⍗ ⍇ ⍈ ⌼ ⌻ ⍃ ⍌ ⍄ ⍁ ⍂ ⍍ ⍠ ⍰ ⍔ ⍞ ⍯ ⌸ ⌹ ⌺ ⎕ ⌧
    environment     = f'{Fore.GREEN}⌼ {Fore.RESET}'
    ready_ally      = f'{Fore.RED}🃢{Fore.RESET}'
    exhauste_ally   = f'{Fore.LIGHTBLACK_EX}🃱{Fore.RESET}'
    ready_upgrade   = f'{Fore.RED}🃪{Fore.RESET}'
    exhauste_upgrade = f'{Fore.LIGHTBLACK_EX}🃪{Fore.RESET}'
    exhauste        = f'{Fore.LIGHTBLACK_EX}🃱{Fore.RESET}'
    hand_card       = f'{Fore.LIGHTBLUE_EX}🂠{Fore.RESET}'
    # on_hand = f'{Fore.LIGHTBLUE_EX}🃵{Fore.RESET}'
    # on_hand = f'{Fore.LIGHTBLUE_EX}🃴{Fore.RESET}'
    scheme          = f'{Fore.CYAN}🃧{Fore.RESET}'
    villain         = f'{Fore.BLACK}🂿{Fore.RESET}'
    minion          = f'{Fore.BLACK}🃟{Fore.RESET}'

    # Resource
    r_b_mental      = f'{Fore.BLUE}B{Fore.RESET}'
    r_r_physical    = f'{Fore.RED}R{Fore.RESET}'
    r_y_energy      = f'{Fore.YELLOW}Y{Fore.RESET}'
    r_g_wild        = f'{Fore.GREEN}G{Fore.RESET}'

    # State
    stunned     = f'{Fore.GREEN}Stunned{Fore.RESET}'
    confused    = f'{Fore.CYAN}Confused{Fore.RESET}'
    tough       = f'{Fore.LIGHTYELLOW_EX}Tough{Fore.RESET}'

    # 🟂🟀🟂🟀✧✶☆
    boost = f'{Fore.RED}✶{Fore.RESET}'
    consequential_damage = '☆'

    # Scheme
    hazard = 'Hazard'
    crisis = 'Crisis'
    acceleration = 'Acceleration'

    # https://stackoverflow.com/questions/14693701/how-can-i-remove-the-ansi-escape-sequences-from-a-string-in-python
    __ansi_escape = re.compile(r'''
        \x1B  # ESC
        (?:   # 7-bit C1 Fe (except CSI)
            [@-Z\\-_]
        |     # or [ for CSI, followed by a control sequence
            \[
            [0-?]*  # Parameter bytes
            [ -/]*  # Intermediate bytes
            [@-~]   # Final byte
        )
    ''', re.VERBOSE)

    @staticmethod
    def Clean(text: str) -> str:
        return Symbol.__ansi_escape.sub('', text)

    @staticmethod
    def CleanPrompt(text: str) -> str:
        text = text.replace(Symbol.r_b_mental, "[[mental]]")
        text = text.replace(Symbol.r_r_physical, "[[physical]]")
        text = text.replace(Symbol.r_y_energy, "[[energy]]")
        text = text.replace(Symbol.r_g_wild, "[[wild]]")
        text = text.replace(Symbol.boost, "[[boost]]")
        return Symbol.Clean(text)

    @staticmethod
    def AddColor(text: str) -> str:
        text = re.sub(r"c(\d+)", "c" + Fore.GREEN + r"\1" + Fore.RESET, text)
        return text

