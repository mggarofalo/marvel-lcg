from . import *

# Track by Scent

def GetAbilities() -> Sequence['Ability']:

    def track_by_scent(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        def action():
            initiator.DrawUp(2, effect)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect, if_this_remove_last_threat=action)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            track_by_scent
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

