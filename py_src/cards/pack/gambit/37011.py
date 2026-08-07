from . import *

# * Bishop: Lucas Bishop

def GetAbilities() -> Sequence['Ability']:

    def bishop(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'energy', effect)

    def bishop_attack(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = effect.cost_func.Get(CostFunc.RemoveEachCounter).return_remove_counter
        message.GainATKForThisAttack(min(2 * value, 6), effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.Response,
            Enemy,
            bishop,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            bishop_attack,
        ).SetCostFunc(CostFunc.RemoveEachCounter("This", 'energy'))
    ]

