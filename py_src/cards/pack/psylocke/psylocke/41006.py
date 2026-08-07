from . import *

# Psionic Redirect

def GetAbilities() -> Sequence['Ability']:

    def psionic_redirect(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        psi_katana = GetYouControlPsiKatana(initiator)
        value = 2 + psi_katana * 2
        message.PreventDamage(value, effect)

        psi_knife = GetYouControlPsiKnife(initiator)
        for _ in range(psi_knife):
            Faces.GiveStatus(effect.targets, "Confused", effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            psionic_redirect,
            is_from_attack=True,
            who_deal_damage=Enemy,
        ).SetPlay().SetLabel('defense')
        .SetTarget("Attacker"),
    ]

