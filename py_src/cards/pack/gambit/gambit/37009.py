from . import *

# Creole Charmer

def GetAbilities() -> Sequence['Ability']:

    def creole_charmer(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def action():
            villain = Worlds.FindVillain(effect)
            if villain:
                Faces.GiveStatus([villain], "Confused", effect)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect, if_this_remove_last_threat=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            creole_charmer
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

