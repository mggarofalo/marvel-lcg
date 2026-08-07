from . import *

# For Whom the Bell Tolls

def GetAbilities() -> Sequence['Ability']:

    def for_whom_the_bell_tolls_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        bell_tower = FindBellTower(effect)
        if bell_tower:
            Faces.RemoveCountersOn([bell_tower], 2, 'chime', effect)
            if bell_tower.card.face.HasTrait('QUIET'):
                player.GetIdentity().TakeDamage(this, 1, effect)
            else:
                scheme = Worlds.FindMainScheme(this)
                if scheme:
                    this.RemoveThreatFromSchemes([scheme], 1, effect)


    def for_whom_the_bell_tolls_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ResolveAbility([this], player, AbilityType.WhenRevealed, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            for_whom_the_bell_tolls_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            for_whom_the_bell_tolls_boost
        ),
    ]

