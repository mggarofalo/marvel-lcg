from . import *

# Manipulation

def GetAbilities() -> Sequence['Ability']:

    def manipulation(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        area = effect.cost_func.Get(CostFunc.Discard).return_original_area[this]
        player = area.play_area
        if player:
            identity = player.GetIdentity()
            player.DiscardRandomHandCards(1, effect)
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            2,
            'invocation',
        ),
        AbilityFactory.AfterCardRemovedCounter(
            AbilityType.ForcedResponse,
            "This",
            'invocation',
            manipulation,
            is_last_counter=True,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

