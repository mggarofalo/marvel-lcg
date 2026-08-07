from . import *

class HasStage(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        # Yes; A1 of Hela is her Stage 1, and A2 of Hela is her stage 2. A villain’s “stage number” corresponds to the roman or arabic numeral in their top right corner. If a villain does not have either, then they have no stage number.
        self.printed_stage = 0
        self.printed_stage_text = 0

        super().__init__(paper)

        def parse(value: str):
            if value == "I":
                self.printed_stage = 1
            elif value == "II":
                self.printed_stage = 2
            elif value == "III":
                self.printed_stage = 3
            elif value == "IV":
                self.printed_stage = 4
            elif value.endswith("1") or value == "A" or value == "B": # Fix "07002", "21013"
                self.printed_stage = 1
            elif value.endswith("2"):
                self.printed_stage = 2
            else:
                self.printed_stage = int(value[0]) # Fix "1A"
            self.printed_stage_text = value
        self.RegisterAttribute("Stage", parse)
        self.RegisterInfoDict('printed_stage')

    @override
    def CheckPrintedValue(self):
        assert self.printed_stage != 0, f"{self.paper.card_id=}"
        return super().CheckPrintedValue()

