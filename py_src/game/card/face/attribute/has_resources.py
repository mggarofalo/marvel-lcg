from . import *

class HasResourceIcon(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_resource_internal = Resources("0")

        super().__init__(paper)

        def parse(value: str):
            self.printed_resource_internal = Resources.FromText(value)
        self.RegisterAttribute("RES", parse)

    @property
    def printed_resource(self) -> 'Resources':
        from game.card.face.base import ClassCard
        message = Message.WhenCountingResourcesOnCards(self.CastTo(ClassCard), self.card.area)
        message.Send()
        return message.return_res
        # return self.printed_resource_internal

