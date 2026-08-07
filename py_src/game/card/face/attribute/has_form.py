from . import *

class HasForm(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.form: str|None = None

        super().__init__(paper)

        self.RegisterAttribute("Form", "form", str)

    @override
    def GetInfoDict(self) -> Dict[str, int]:
        info: Dict[str, int] = {}
        if self.form:
            info |= {f'f_{self.form}': 1}
        return info | super().GetInfoDict()

