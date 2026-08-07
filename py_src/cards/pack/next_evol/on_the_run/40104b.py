from . import *

# Escaping with Hope

def GetAbilities() -> Sequence['Ability']:

    def escaping_with_hope(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Worlds.SetGameOver(True, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("MARAUDER", Minion),
            guard=1,
            steady=1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            steady=1,
            expert_mode_only=True,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.NonKeywordBold,
            Villain,
            escaping_with_hope
        ),
    ]

