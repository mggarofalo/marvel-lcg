from . import *

# Impersonation

def GetAbilities() -> Sequence['Ability']:

    def impersonation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action():
            YouMayFlipToYourAlterEgoForm(initiator, effect)

        this.RemoveThreatFromSchemes(effect.targets, 4, effect, if_this_remove_last_threat=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            impersonation
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

