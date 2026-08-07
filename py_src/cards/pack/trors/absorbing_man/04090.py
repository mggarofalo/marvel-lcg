from . import *

# Icy Grip

def GetAbilities() -> Sequence['Ability']:

    def icy_grip_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)
        if AbsorbingManHasTrait(effect, 'ICE'):
            player.GetIdentity().TakeIndirectDamage(this, 2, effect)

    def icy_grip_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if AbsorbingManHasTrait(effect, 'ICE', 'METAL'):
            Faces.GiveStatus([GetAbsorbingMan(effect)], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            icy_grip_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            icy_grip_boost
        ),
    ]

