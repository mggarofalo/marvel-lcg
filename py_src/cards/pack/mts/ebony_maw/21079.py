from . import *

# Rubblestorm

def GetAbilities() -> Sequence['Ability']:

    def rubblestorm(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        area = effect.cost_func.Get(CostFunc.Discard).return_original_area[this]
        player = area.play_area
        if player:
            this.DealDamage(player.GetControlCharacters(), 2, effect)

    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            3,
            'invocation',
        ),
        AbilityFactory.AfterCardRemovedCounter(
            AbilityType.ForcedResponse,
            "This",
            'invocation',
            rubblestorm,
            is_last_counter=True,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

