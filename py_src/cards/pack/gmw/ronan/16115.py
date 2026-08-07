from . import *

# Kree Physiology

def GetAbilities() -> Sequence['Ability']:

    def kree_physiology_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect)
        if villain:
            if villain.IsTough():
                player.GetIdentity().TakeDamage(this, 1, effect)
            else:
                Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kree_physiology_revealed
        ),
    ]

