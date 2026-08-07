from core import *
from engine.lib import Json
from engine.config import ConfigVariables

TRANSLATE_FILE = ConfigVariables.File('translate_file', "")

class TransText:
    translations: Dict[str, str] = {}
    translate_file: str = ""

    @staticmethod
    def Initialize() -> None:
        translate_file = TRANSLATE_FILE.value
        TransText.translate_file = translate_file
        if translate_file:
            TransText.translations = Json.Load(translate_file)

    def __init__(self, text: str, **kwargs: Any) -> None:
        from game.render.symbol import Symbol
        self.is_translated = False

        try:
            if text in TransText.translations:
                found_string = TransText.translations[text]
                self.text_symbol_internal = found_string.format(**kwargs)
                self.is_translated = True
            else:
                self.text_symbol_internal = text.format(**kwargs)
        except:
            self.text_symbol_internal = text

        self.text_no_symbol_internal = Symbol.CleanPrompt(self.text_symbol_internal)

    @property
    def text_symbol(self) -> str:
        return self.text_symbol_internal

    @property
    def text_no_symbol(self) -> str:
        return self.text_no_symbol_internal

