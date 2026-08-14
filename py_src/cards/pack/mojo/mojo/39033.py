from . import *

# Director's Directions

def GetAbilities() -> Sequence['Ability']:

    def directors_directions_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        mojo = Worlds.FindVillain(effect, name="Mojo")

        def take_damage():
            num = identity.GetTokens('threat')
            damage_message = identity.TakeDamage(this, num, effect)
            if not damage_message or damage_message.took_damage < 2:
                ThisCardGainSurge(effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("2", different_type=True)
            ),
            AbilityFactory.ForChoiceAbility(
                "Mojo scheme",
                lambda targets:
                    targets[0].CastTo(Villain).DoSchemes(player, effect)
            ).SetTarget(Villain) if mojo != None else None,
            AbilityFactory.ForChoiceAbility(
                "Take 1 damage for each threat on your identity",
                lambda targets:
                    take_damage()
            ).SetTarget("YourIdentity")
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            directors_directions_revealed
        ),
    ]

