from . import *

# Clawed Strike

def GetAbilities() -> Sequence['Ability']:

    def clawed_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 4, effect)

        initiator.ResolveSpecialAbility(effect.targets2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            clawed_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2(Upgrade, trait="BLACK PANTHER", from_where=["YouControlCards"]),
    ]

