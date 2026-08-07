from . import *

# Wrapped in Metal

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            NonEliteMinion
        ).SetPlay(hero_form_only=True),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            treat_as_blank=True,
        ),
        AbilityFactory.EnemyCannotActivate(
            AbilityType.NonKeyword,
            "AttachedMinion",
        )
    ]

