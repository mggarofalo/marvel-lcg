from . import *

# Electric Whip Attack

def GetAbilities() -> Sequence['Ability']:

    def electric_whip_attack_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        damage = len(identity.GetAttachedUpgrades())

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                f"deal {damage} damage to your hero",
                lambda targets:
                    this.DealDamage(targets, damage, effect)
            ).SetTarget("YourHero"),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard an upgrade you control",
                lambda targets:
                    Faces.DiscardAll(targets, effect),
                condition=not not identity.GetAttachedUpgrades(),
            ).SetTarget(Upgrade, from_where=["YouControlCards"], canbe_discard=True)
        )

    def electric_whip_attack_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        player = message.GetToPlayer()

        player.DiscardControlCards(effect, upgrade=True)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            electric_whip_attack_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            electric_whip_attack_boost,
            is_undefended_attack=True,
            attacker=Villain,
        ),
    ]

