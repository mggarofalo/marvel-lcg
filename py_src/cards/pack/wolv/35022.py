from . import *

# * Weapon X

def GetAbilities() -> Sequence['Ability']:

    def weapon_x(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        face = initiator.DiscardDeckUntil(
            effect,
            card_class="IdentitySpecific"
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="MUTANT"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            weapon_x
        ).SetCostFunc(CostFunc.TakeDamage(1, "YourIdentity"))
        .SetCostFunc(CostFunc.Exhaust("This"))
    ]

