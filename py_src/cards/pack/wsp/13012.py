from . import *

# * Wasp: Janet Van Dyne

def GetAbilities() -> Sequence['Ability']:

    def wasp(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        if message.overpaid > 0:
            overpaid = min(message.paid_cost.GetColor("Y"), message.overpaid)

            value = min(4, overpaid)
            Faces.PlaceCountersOn([this], value, 'pym', effect)


    return [
        AbilityFactory.WhenCardEnterPlay(
            AbilityType.Interrupt,
            "This",
            wasp
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Ally).GetCounters('pym'),
            health=1,
            change_on_event=OnEvent.Counter("This", 'pym')
        )
    ]

