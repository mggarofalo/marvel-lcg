from . import *

# Earthquake

def GetAbilities() -> Sequence['Ability']:

    def earthquake_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards(("Zero", 2), effect)
        identity = player.GetIdentity()
        Faces.ExhaustAll([identity], effect)

    def earthquake_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("RR")
            ),
            AbilityFactory.ForChoiceAbility(
                "Exhaust your identity",
                lambda targets:
                    Faces.ExhaustAll(targets, effect),
            ).SetTarget("YourIdentity", canbe_exhaust=True),
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            earthquake_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            earthquake_boost
        )
    ]

