from . import *

# Swift Retribution

def GetAbilities() -> Sequence['Ability']:

    def swift_retribution(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        for target in effect.targets:
            target.CastTo(Villain).DoSchemes(initiator, effect)
        this.DealDamage(effect.targets, 4, effect)

        initiator = effect.GetInitiator()


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swift_retribution
        ).SetPlay().SetLabel('attack')
        .SetTarget(Villain),
    ]

