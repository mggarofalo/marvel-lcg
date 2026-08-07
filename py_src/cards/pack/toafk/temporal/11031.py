from . import *

# Chitauri Soldier

def GetAbilities() -> Sequence['Ability']:

    def chitauri_soldier(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        for target in message.attacked_targets:
            player = target.GetControlByPlayer()

            faces = Worlds.DiscardEncounterCards(1, effect)
            damage = FacesCounter.CountTotalBoostIcons(faces)
            player.GetIdentity().TakeIndirectDamage(this, damage, effect)

    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            chitauri_soldier,
        )
    ]

