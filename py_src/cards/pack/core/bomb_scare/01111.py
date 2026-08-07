from . import *

# Explosion

def GetAbilities() -> Sequence['Ability']:

    def explosion(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Bomb Scare",
            card_type=SchemeSide2
        )
        if face:
            damage = face.threat
            units: List['Unit2'] = []
            units += Worlds.GetOnFieldHeroes(effect)
            units += Worlds.GetOnFieldAllies(effect)
            player.AssignDamage(units, this, damage, effect)
        else:
            this.GainSurge(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            explosion
        )
    ]
