from . import *

# * Hobgoblin

def GetAbilities() -> Sequence['Ability']:

    def hobgoblin(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        identity = player.GetIdentity()

        message.SetBeInstead(effect)

        atk = this.attack
        faces = Worlds.DiscardEncounterCards(atk, effect)

        damage = FacesCounter.CountTotalBoostIcons(faces)
        identity.TakeIndirectDamage(this, damage, effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            hobgoblin
        ),
    ]

