from . import *

# * Tempo

def GetAbilities() -> Sequence['Ability']:

    def tempo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlUpgrade()
        face = Filter.One(faces, effect, highest_cost=True)
        if face:
            Faces.ReturnToHand([face], "Owner", effect)

    def tempo(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if not message.attacker:
            return True

        if message.attacker.HasTrait("TINY"):
            return False

        return True

    return [
        AbilityFactory.ReduceCharacterTakeDamageBy(
            "This",
            1,
            from_each_attack=True,
            conditions=[tempo]
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            tempo_revealed
        ),
    ]

