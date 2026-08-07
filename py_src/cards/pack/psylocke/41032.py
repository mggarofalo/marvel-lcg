from . import *

# Psi-Flail Strike

def GetAbilities() -> Sequence['Ability']:

    def psi_flail_strike(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "You",
            psi_flail_strike,
            attacker=Enemy
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC").SetLabel('attack')
        .SetTarget("Attacker"),
    ]

