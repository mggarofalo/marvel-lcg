from . import *

# * Ant-Man

def GetAbilities() -> Sequence['Ability']:

    def ant_man(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = min(4, message.overpaid)
        Faces.PlaceCountersOn([this], value, 'pym', effect)


    return [
        AbilityFactory.WhenCardEnterPlay(
            AbilityType.Interrupt,
            "This",
            ant_man
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('pym'),
            health=1,
            change_on_event=OnEvent.Counter("This", 'pym')
        )
    ]

