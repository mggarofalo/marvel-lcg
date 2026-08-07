from . import *

# Widow's Bite

def GetAbilities() -> Sequence['Ability']:

    def widows_bite_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            damage = 2
        else:
            damage = 1
        Faces.GiveStatus([identity], "Stunned", effect)
        identity.TakeDamage(this, damage, effect)

    def widows_bite_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            if message.attacker:
                Faces.GiveStatus([message.attacker], "Stunned", effect)
        message.AfterThisAttack(action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            widows_bite_revealed
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            widows_bite_preparation
        ),
    ]

