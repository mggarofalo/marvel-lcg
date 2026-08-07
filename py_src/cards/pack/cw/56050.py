from . import *

# Cuts Both Ways

def GetAbilities() -> Sequence['Ability']:

    def cuts_both_ways(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetHero().GainUntilPhaseEnd(
            effect,
            retaliate=1,
        )


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            cuts_both_ways
        ).SetPlay().SetLabel('defense'),
    ]

