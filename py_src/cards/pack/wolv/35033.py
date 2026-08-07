from . import *

# * Longshot

def GetAbilities() -> Sequence['Ability']:

    def longshot(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if EncounterNonVillainCard.IsType(face) and face.boost_star:
            Faces.DefeatUnits(effect.targets, this, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            "This",
            NonEliteMinion,
            longshot
        ).SetTarget("Targets", has_non_health=False),
    ]

