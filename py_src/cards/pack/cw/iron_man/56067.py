from . import *

# Mark V Helmet

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Iron Man"),
            when_attach_operation=lambda face, effect:
                Faces.GiveStatus([face], "Tough", effect)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Iron Man"),
            stalwart=1,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Iron Man"),
        ).SetCost(Cost("BR")),
    ]

