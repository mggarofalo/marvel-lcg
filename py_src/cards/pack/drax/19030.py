from . import *

# "Bring It!"

def GetAbilities() -> Sequence['Ability']:

    def bring_it(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        size = len(initiator.GetEngagedMinions())
        initiator.DrawUp(size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bring_it
        ).SetPlay().SetLabel()
        .MaxOnePerPhase(),
    ]

