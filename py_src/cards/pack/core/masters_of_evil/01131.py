from . import *

# * Tiger Shark

def GetAbilities() -> Sequence['Ability']:

    def tiger_shark_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)
        
        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)

    def tiger_shark(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tiger_shark_boost
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            tiger_shark
        )
    ]

