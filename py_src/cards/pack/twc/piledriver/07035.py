from . import *

# Distracting Taunts

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=PILEDRIVER)
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name=PILEDRIVER),
            health=3,
        ),
        AbilityFactory.PlayersCannotAttackWhile(
            "AnyPlayer",
            Villain,
            conditions=[
                lambda effect, message:
                    effect.this.GetBindFace() != message.being_attack,
            ]
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            Hero,
            CardFinder(name=PILEDRIVER),
            DiscardThisCard,
        ).SetCost(Cost("RR"))
    ]

