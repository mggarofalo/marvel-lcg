from . import *

# Have at Thee!

def GetAbilities() -> Sequence['Ability']:

    def have_at_thee(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        overkill = False
        for target in effect.targets:
            if HAS_DEATH_GLOW_FINDER.Check(target):
                overkill = True

        hero.DealDamage(effect.targets, 7, effect, property=AttackProperty(overkill=overkill))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            have_at_thee
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

