from . import *

# * Ghost-Spider

def GetAbilities() -> Sequence['Ability']:

    def ghost_spider(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            Event,
            ghost_spider,
            which_abilities=["Interrupt", "Response"],
        ).SetTarget("This", canbe_ready=True)
        .SetName("Dizzying Reflexes")
        .LimitOncePerPhase(),
    ]

