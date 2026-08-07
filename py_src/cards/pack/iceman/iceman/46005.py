from . import *

# Cryokinetic Perception

def GetAbilities() -> Sequence['Ability']:

    def cryokinetic_perception(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.DrawUp(1, effect)

        if CardFinder2("ICE").Checks(faces):
            Faces.ReadyAll([initiator.GetIdentity()], effect)

    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            None,
            cryokinetic_perception,
            ability_name='"Freeze!"',
            control_by_you=True,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

