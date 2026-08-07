from . import *

# Energy Projectiles

def GetAbilities() -> Sequence['Ability']:

    def energy_projectiles(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        faces = player.GetControlCharacters()

        this.DealDamage(faces, 1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            energy_projectiles
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            energy_projectiles
        ).SetTarget("DefendingCharacter", range="All")
    ]

