from . import *

# Falcon's Flock

def GetAbilities() -> Sequence['Ability']:

    # def falcon_s_flock(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> None:
    #     this = effect.this.CastTo(Support)
    #     Unused(this)

    #     face = message.paying_card
    #     if face:
    #         Stores.AddFace('falcon_s_flock', face, effect)


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("Y"),
            CardFinder2("AERIAL"),
            # conditions=[
            #     lambda effect, message:
            #         message.paying_for_card not in Stores.GetFaces('falcon_s_flock', effect)
            # ],
            # ex_operation=falcon_s_flock
        ).SetCostFunc(CostFunc.Counter("This", 1, 'bird'))
        # .SetToAvailableTimesFn(lambda effect, message: effect.this.CastTo(CanPlaceCounter).GetCounters('bird'))
    ]

