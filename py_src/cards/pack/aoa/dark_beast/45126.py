from . import *

# Time-Travel Shenanigans

def GetAbilities() -> Sequence['Ability']:

    def time_travel_shenanigans(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        env = Worlds.FindCardOnField(
            effect,
            trait="SETTING",
            card_type=Environment,
        )

        if env:
            set_name = env.paper.set_name

            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                set_name=set_name
            )
            if face:
                face.Reveal(player, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            time_travel_shenanigans,
            has_defeating_player=True
        ),
    ]

