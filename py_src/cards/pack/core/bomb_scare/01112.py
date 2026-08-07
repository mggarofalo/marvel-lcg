from . import *

# False Alarm

def GetAbilities() -> Sequence['Ability']:

    def false_alarm(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        identity = message.GetToPlayer().GetIdentity()

        if identity.IsConfused():
            this.GainSurge(1, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            false_alarm
        )
    ]
