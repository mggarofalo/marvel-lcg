from . import *

# * Shadowcat

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitIgnoreKeywordIcons(
            "You",
            guard=True,
            patrol=True,
            crisis=True,
            conditions=[
                lambda effect, message:
                    Condition.PlayerInMassForm("You", "Phased", effect)
            ],
        ).SetName("Selective Intangibility"),
    ]

