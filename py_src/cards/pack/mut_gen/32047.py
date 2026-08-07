from . import *

# Aggressive Energy

def GetAbilities() -> Sequence['Ability']:

    def aggressive_energy(effect: 'Effect', message: 'Message.WhenCardBeSpendAsResource') -> None:
        this = effect.this.CastTo(Resource)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldAttack(
                AbilityType.Temp0,
                "You",
                lambda effect, atk_message:
                    atk_message.DealAdditionalDamage(1, effect),
                by_effect=message.for_effect,
            ),
            unregister_after_exec=False,
            until_resolve_effect=message.for_effect
        )

    return [
        AbilityFactory.WhenYouSpendThisCardToPlay(
            AbilityType.HeroInterrupt,
            aggressive_energy,
            CardFinder2("ATTACK", Event),
        ),
    ]

