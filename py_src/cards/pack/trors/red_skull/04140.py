from . import *

# The Sleeper Awakened

def GetAbilities() -> Sequence['Ability']:

    def the_sleeper_awakened_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        face = Search.SearchForCard(
            effect,
            first_player,
            include_set_aside=True,
            name="The Sleeper",
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(first_player, effect)


    return [
        AbilityFactory.SchemeCannotLeavlPlayWhile(
            "This",
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="The Sleeper",
                        card_type=Minion
                    ) != None
            ]
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            the_sleeper_awakened_revealed
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.NonKeyword,
            None,
            RemoveThisFromGame,
            conditions=[
                lambda effect, message:
                    message.trigger.IsName("The Sleeper"),
            ]
        ),
    ]

