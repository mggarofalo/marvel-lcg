from . import *

# * Sauron

def GetAbilities() -> Sequence['Ability']:

    def sauron_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Life Drain",
            card_type=Attachment,
        )
        if face:
            face.Reveal(player, effect)

    def sauron_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        taregts = [message.activating_enemy]
        this.HealthUnits(taregts, 3, effect)
        Faces.GiveStatus(taregts, "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sauron_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sauron_boost
        ),
    ]

