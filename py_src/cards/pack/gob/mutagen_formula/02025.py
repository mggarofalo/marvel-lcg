from . import *

# * Monster

def GetAbilities() -> Sequence['Ability']:

    def monster(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        identity = message.GetToPlayer().GetIdentity()
        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            monster
        ),
        AbilityFactory.WhenBoostShuffleThisIntoEncounterDeckAfterActivationEnd()
    ]

