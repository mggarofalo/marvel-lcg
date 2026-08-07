from . import *

# * Drax

def GetAbilities() -> Sequence['Ability']:

    def drax(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        if this.GetCounters('vengeance') < 3:
            Faces.PlaceCountersOn([this], 1, 'vengeance', effect)
        else:
            initiator = effect.GetInitiator()
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Hero).GetCounters('vengeance'),
            attack=1,
            change_on_event=OnEvent.Counter("This", 'vengeance')
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            Villain,
            "This",
            drax
        ).SetTarget("This")
    ]

