from . import *

# Energy Drain

def GetAbilities() -> Sequence['Ability']:

    def energy_drain_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("YY"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Exhaust your identity",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YourIdentity", canbe_exhaust=True)
        )

    def energy_drain_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("YY"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Take 3 damage",
                lambda targets:
                    identity.TakeDamage(this, 3, effect)
            ).SetTarget("YourIdentity")
        )

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            energy_drain_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            energy_drain_hero
        ),
    ]

