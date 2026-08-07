from . import *

# Tentacle Strike

def GetAbilities() -> Sequence['Ability']:

    def tentacle_strike(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        for target in effect.targets:
            damage = 1
            if Unit2.IsType(target) and target.IsStunned():
                damage = 4
            Faces.GiveStatus([target], "Stunned", effect)
            this.DealDamage([target], damage, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tentacle_strike
        ).SetTarget("YourIdentity"),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tentacle_strike
        ).SetTarget("YourIdentity"),
    ]

