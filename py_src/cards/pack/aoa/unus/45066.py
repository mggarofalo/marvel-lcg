from . import *

# Genetic Experiments

def GetAbilities() -> Sequence['Ability']:

    def genetic_experiments(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 2, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder2("INFINITE", Minion),
            if_cannot_gain_surge=True,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=2,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            genetic_experiments
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder2("INFINITE", Minion)
        ),
    ]

