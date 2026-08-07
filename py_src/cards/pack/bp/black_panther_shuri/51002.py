from . import *

# * T'Challa

def GetAbilities() -> Sequence['Ability']:

    def tchalla(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)

    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "This",
            tchalla
        ).SetTarget(Upgrade, trait="BLACK PANTHER", from_where=["YouControlCards"]),
    ]

