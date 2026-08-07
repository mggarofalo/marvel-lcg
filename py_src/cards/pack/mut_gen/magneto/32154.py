from . import *

# Metal Shards

def GetAbilities() -> Sequence['Ability']:

    def metal_shards_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlCharacters()
        this.DealDamage(faces, 1, effect)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            Faces.PlaceCountersOn([scheme], 1, 'magnet', effect)

    def metal_shards_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(face: 'CardFace'):
            scheme = Worlds.FindMainScheme(effect)
            if scheme:
                Faces.PlaceCountersOn([scheme], 1, 'magnet', effect)

        message.would_atk_message.IfThisAttackDefeats(Ally, action, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            metal_shards_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            metal_shards_boost,
            during_attack=True,
        ),
    ]

