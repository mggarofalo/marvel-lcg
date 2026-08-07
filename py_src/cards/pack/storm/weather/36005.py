from . import *

# Blizzard

def GetAbilities() -> Sequence['Ability']:

    def blizzard(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            target.TreatAsIfBlank(effect, until_round_end=True)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            blizzard
        ).SetTarget(Minion, non_trait="ELITE"),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            attack=-1,
        )
    ]

