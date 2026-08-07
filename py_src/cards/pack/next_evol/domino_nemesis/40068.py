from . import *

# * Prototype

def GetAbilities() -> Sequence['Ability']:

    def prototype_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        value = identity.sustained
        Faces.PlaceCountersOn([this], value, 'luck', effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Minion).GetCounters('luck'),
            health=1,
            change_on_event=OnEvent.Counter("This", 'luck')
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            prototype_revealed
        ),
    ]

