from . import *

# Fireball

def GetAbilities() -> Sequence['Ability']:

    def fireball(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        area = effect.cost_func.Get(CostFunc.Discard).return_original_area[this]
        player = area.play_area
        if player:
            identity = player.GetIdentity()
            this.DealDamage([identity], 4, effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            4,
            'invocation',
        ),
        AbilityFactory.AfterCardRemovedCounter(
            AbilityType.ForcedResponse,
            "This",
            'invocation',
            fireball,
            is_last_counter=True,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

