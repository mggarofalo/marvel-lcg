from . import *

# Talon Line

def GetAbilities() -> Sequence['Ability']:

    def talon_line(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        face = initiator.GetIdentity().GetBuff(Buff_53001a).face

        if face:
            value = FacesCounter.CountTotalBoostStarAndBoost([face])
        else:
            value = 0

        for _ in range(value):
            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Ready a character you control",
                    lambda targets:
                        Faces.ReadyAll(targets, effect)
                ).SetTarget("YouControlUnit", canbe_ready=True),
                AbilityFactory.ForChoiceAbility(
                    "Stun an enemy",
                    lambda targets:
                        Faces.GiveStatus(targets, "Stunned", effect)
                ).SetTarget(Enemy, canbe_stunned=True)
            )

    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder(name="Falcon"),
            talon_line,
            ability_name="Eagle-Eyed",
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

