from . import *

# Get Over Here!

def GetAbilities() -> Sequence['Ability']:

    def get_over_here(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 1, effect)
        if initiator.GetHero().HasTrait('AERIAL'):
            for target in effect.targets:
                if target.IsInPlay():
                    target.CastTo(Minion).EngagePlayer(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            get_over_here
        ).SetPlay().SetLabel('attack')
        .SetTarget(Minion),
    ]

