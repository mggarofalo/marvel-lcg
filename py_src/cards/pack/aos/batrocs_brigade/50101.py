from . import *

# Batroc's Brigade

def GetAbilities() -> Sequence['Ability']:

    def batrocs_brigade_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        units = Worlds.GetOnFieldEnemies(effect)
        Faces.GiveStatus(units, "Tough", effect)

    def batrocs_brigade(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.CancelAllEffectsAndDiscardIt(effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            toughness=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            batrocs_brigade_revealed
        ),
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "You",
            NonEliteMinion,
            "All",
            batrocs_brigade
        ).SetCost(Cost("3"))
    ]

