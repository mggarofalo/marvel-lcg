from . import *

# Army of the Controlled

def GetAbilities() -> Sequence['Ability']:

    def army_of_the_controlled_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        Find.FindAndPutIntoPlay(
            effect,
            player,
            name="Controlled Innocents",
            card_type=Environment,
        )

    def army_of_the_controlled(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            name="Controlled Minion",
        )
        Faces.DiscardAll(faces, effect)

        player = Worlds.GetFirstPlayer(effect)
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'all-purpose', effect)
            ).SetTarget(Support, repeat_rules="Any", range=(1, len(faces)))
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            army_of_the_controlled_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            army_of_the_controlled,
        ),
    ]

