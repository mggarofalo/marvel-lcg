from . import *

# T'Challa's Shadow

def GetAbilities() -> Sequence['Ability']:

    def tchallas_shadow(effect: 'Effect', message: 'Message.AfterUnitThwartEnd|Message.AfterUnitAttackEnd|Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'doubt', effect)


    return [
        AbilityFactory.IncreaseResourceCostToPlay(
            CardFace,
            +1,
            "You",
        ),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.ForcedResponse,
            "You",
            tchallas_shadow
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "You",
            tchallas_shadow
        ),
        AbilityFactory.AfterUnitDefendEnd(
            AbilityType.ForcedResponse,
            "You",
            tchallas_shadow
        ),
    ]

