from . import *

# * White Widow: Yelena Belova

def GetAbilities() -> Sequence['Ability']:

    def white_widow(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        face = message.effect.this

        value = FacesCounter.GetPrintedResourceCost([face])
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            ("YouControl", CardFinder(trait="PREPARATION")),
            white_widow,
        ).SetTarget("This", canbe_heal=True),
    ]

