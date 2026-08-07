from . import *

# Ozymandias

def GetAbilities() -> Sequence['Ability']:

    def ozymandias_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 3 threat on Ancient Ritual",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 3, effect)
            ).SetTarget(name="Ancient Ritual"),
            AbilityFactory.ForChoiceAbility(
                "This card gains [boost][boost][boost]",
                lambda targets:
                    this.GainBoostIcons(3, effect)
            )
        )

    return [
        AbilityFactory.PlaceThreatOnCardWhenThisScheme(
            AbilityType.NonKeywordStar,
            CardFinder(name="Ancient Ritual"),
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ozymandias_boost
        ),
    ]

