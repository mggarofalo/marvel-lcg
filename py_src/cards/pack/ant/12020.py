from . import *

# Swarm Tactics

def GetAbilities() -> Sequence['Ability']:

    def swarm_tactics(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        hero.ChangeToOtherHeroForm(effect)
        Faces.ReadyAll([hero], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swarm_tactics
        ).SetPlay()
        .SetTarget("TeamUp")
    ]

