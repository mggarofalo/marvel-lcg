from . import *

# All in a Day's Work

def GetAbilities() -> Sequence['Ability']:

    def all_in_a_day_s_work(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if CriminalEnterpriseExist(effect):
            CriminalEnterprisePlaceInfamy(+2, effect)
        else:
            StateofMadnessPlaceMadness(-2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            all_in_a_day_s_work
        ),
        BoostPlaceInfamyOrRemoveMadness()
    ]
