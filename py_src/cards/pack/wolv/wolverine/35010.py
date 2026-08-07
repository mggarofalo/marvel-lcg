from . import *

# Lunging Strike

def GetAbilities() -> Sequence['Ability']:

    def lunging_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        hero = effect.GetInitiator().GetHero()
        overkill = False

        if type(message) is Message.WhenPlayerLikeInTurn and \
            message.by_effect.this.IsName("Wolverine's Claws"):
            overkill = True

        hero.DealDamage(effect.targets, 8, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lunging_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

