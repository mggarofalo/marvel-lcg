from . import *

# * Eros

def GetAbilities() -> Sequence['Ability']:

    def eros(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)

    def get_max(effect: 'Effect'):
        message = effect.bind_message
        if isinstance(message, Message.AfterPlayerPlayedCard):
            return message.paid_resources.GetColor("B")
        return 0

    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            eros,
        ).SetTarget(Minion, canbe_confused=True,
            range=(1, get_max)),
    ]

