from . import *

# * Magneto's Cape

def GetAbilities() -> Sequence['Ability']:

    def magnetos_cape(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Magneto"),
            trait="AERIAL",
        ),
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.Response,
            "You",
            None,
            magnetos_cape,
            ability_name="Magnetic Pull",
            control_by_you=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Magneto", canbe_ready=True),
    ]

