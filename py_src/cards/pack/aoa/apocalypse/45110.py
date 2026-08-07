from . import *

# Wolf Among Sheep

def GetAbilities() -> Sequence['Ability']:

    def wolf_among_sheep_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        
        face = Worlds.FindCardOnField(
            effect,
            trait="PRELATE",
            card_type=Minion
        )
        if not face:
            face = Worlds.FindCardOnField(
                effect,
                name="Apocalypse",
                card_type=Enemy
            )

        if face:
            face.DoActivate(player, effect)

    def wolf_among_sheep_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            trait="PRELATE",
            card_type=Minion
        )
        if not face:
            face = Worlds.FindCardOnField(
                effect,
                name="Apocalypse",
                card_type=Enemy
            )

        if face:
            Faces.GiveStatus([face], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wolf_among_sheep_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            wolf_among_sheep_boost
        ),
    ]

