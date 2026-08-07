from . import *

# Emergency

def GetAbilities() -> Sequence['Ability']:

    def emergency(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.WhenSchemeWouldPlaceThreat(
                AbilityType.Temp0,
                None,
                lambda effect, message:
                    message.ReduceThreat(1, effect),
                by_scheme=message
            ),
            unregister_after_exec=True,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Interrupt,
            Villain,
            emergency,
        ).SetPlay().SetLabel('thwart')
    ]
