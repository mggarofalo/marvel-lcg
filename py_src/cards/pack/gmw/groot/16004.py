from . import *

# "I. AM. GROOT!"

def GetAbilities() -> Sequence['Ability']:

    def i_am_groot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetIdentity().GetCounters('growth')
        this.DealDamage(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            i_am_groot
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

