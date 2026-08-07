from . import *

# Electromagnetic Blast

def GetAbilities() -> Sequence['Ability']:

    def electromagnetic_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlCardsByType(upgrade=True, support=True)
        Faces.ExhaustAll(faces, effect)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            Faces.PlaceCountersOn([scheme], 1, 'magnet', effect)

    def electromagnetic_blast_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.ExhaustAll([identity], effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            electromagnetic_blast_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            electromagnetic_blast_boost
        ),
    ]

