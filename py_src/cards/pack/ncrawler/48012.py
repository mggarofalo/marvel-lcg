from . import *

# * Rogue: Anna Marie

def GetAbilities() -> Sequence['Ability']:

    def rogue(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        units = effect.cost_func.Get(CostFunc.DealDamage).return_damaged_units

        traits: List["CardFace.TRAITS"] = []
        thwart = 0
        attack = 0
        for unit in units:
            if HasThwart.IsType(unit):
                thwart += unit.base_thwart
            if HasAttack.IsType(unit):
                attack += unit.base_attack
            traits += unit.traits

        this.GainUntilRoundEnd(
            effect,
            trait=traits,
            thwart=thwart,
            attack=attack
        )



    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            rogue
        ).SetCostFunc(CostFunc.DealDamage(1,
            card_type=Friend,
            another=True))
        .LimitOncePerRound(),
    ]

