from . import *

# Seeking Vengeance

def GetAbilities() -> Sequence['Ability']:

    def seeking_vengeance_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Lady Deathstrike",
            card_type=Enemy,
        )
        if face:
            face.DoActivate(player, effect)
        else:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                name="Lady Deathstrike",
                card_type=Enemy,
            )
            if face:
                face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            seeking_vengeance_revealed
        ),
    ]

