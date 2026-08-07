from . import *

# * Black Panther

def GetAbilities() -> Sequence['Ability']:

    def black_panther(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ResolveSpecialAbility(effect.targets, effect)

    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            black_panther
        ).SetTarget(Upgrade, trait="BLACK PANTHER", from_where=["YouControlCards"]),
    ]

