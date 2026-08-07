from . import *

# Prelate Sidearm

def GetAbilities() -> Sequence['Ability']:

    def prelate_sidearm(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Unus")
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            CardFinder(name="Unus"),
            Ally,
            prelate_sidearm,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Unus"),
        ).SetCost(Cost("YR")),
    ]

