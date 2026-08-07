from . import *

# Overburdened

def GetAbilities() -> Sequence['Ability']:

    def overburdened(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard 1 resource card from your hand",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(Resource, from_where=["YourHandCards"], canbe_discard=True),
            AbilityFactory.ForChoiceAbility(
                "Take 2 damage",
                lambda targets:
                    identity.TakeDamage(this, 2, effect)
            ).SetTarget("YourIdentity")
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            overburdened
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            overburdened
        ),
    ]

