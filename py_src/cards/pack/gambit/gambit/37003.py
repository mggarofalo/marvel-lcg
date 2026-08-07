from . import *

# * The Thieves Guild

def GetAbilities() -> Sequence['Ability']:

    def the_thieves_guild(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        def action():
            initiator.DrawUp(1, effect)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect, if_this_remove_last_threat=action)


    return [
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.AlterEgoResponse,
            "You",
            None,
            the_thieves_guild,
            ability_name="Thief Extraordinaire",
            control_by_you=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

