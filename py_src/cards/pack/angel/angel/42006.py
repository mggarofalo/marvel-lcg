from . import *

# Natural Flight

def GetAbilities() -> Sequence['Ability']:

    def natural_flight(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 4, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            natural_flight,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetIgnoreKeyword('Crisis', 'Patrol',
            condition=lambda effect:
                effect.GetInitiator().GetIdentity().IsName("Angel")
            ),
    ]

