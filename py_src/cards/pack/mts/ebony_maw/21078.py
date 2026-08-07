from . import *

# Pacification

def GetAbilities() -> Sequence['Ability']:

    def pacification(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        area = effect.cost_func.Get(CostFunc.Discard).return_original_area[this]
        player = area.play_area
        if player:
            identity = player.GetIdentity()
            Faces.ExhaustAll(player.GetControlUpgrade(), effect)
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            3,
            'invocation',
        ),
        AbilityFactory.AfterCardRemovedCounter(
            AbilityType.ForcedResponse,
            "This",
            'invocation',
            pacification,
            is_last_counter=True,
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

