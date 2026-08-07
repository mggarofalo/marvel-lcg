from . import *

# Defense Mechanism

def GetAbilities() -> Sequence['Ability']:

    def defense_mechanism(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ResolveSpecialAbility(initiator, effect, name="Spider Camouflage")

    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            defense_mechanism
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

