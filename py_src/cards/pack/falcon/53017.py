from . import *

# * Hugin & Munin

def GetAbilities() -> Sequence['Ability']:

    def hugin_munin(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        faces = effect.cost_func.Get(CostFunc.SearchForCard).searched_faces
        value = FacesCounter.CountTotalBoostStarAndBoost(faces)
        units = effect.targets[:value]
        Faces.ReadyAll(units, effect)



    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            hugin_munin
        ).SetCostFunc(CostFunc.SearchForCard(Minion, "PutIntoPlayEngagedYou", encounter_deck_top=10))
        .SetTarget("YouControlUnit", canbe_ready=True, range=(1, "All")),
    ]

