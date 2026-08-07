from . import *

# On the Double

def GetAbilities() -> Sequence['Ability']:

    def on_the_double(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            on_the_double
        ).SetPlay().SetLabel()
        .SetTarget(Support, trait="S.H.I.E.L.D", combined_printed_cost=(0, 6), canbe_ready=True),
    ]

