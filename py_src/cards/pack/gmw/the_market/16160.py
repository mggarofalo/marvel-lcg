from . import *

# In Harm's Way

def GetAbilities() -> Sequence['Ability']:

    def in_harms_way(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().TakeDamage(this, 2, effect)
        this.RemoveThreatFromSchemes(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            in_harms_way
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

