from . import *

# Intimidation

def GetAbilities() -> Sequence['Ability']:

    def intimidation_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        message.GiveBoostCardForThisActivation(Villain, 1, effect)

    def intimidation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        player = message.GetToPlayer()
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("2"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Give the villain 1 facedown boost card",
                lambda targets:
                    Faces.GiveFacedownBoostCards(targets, 1, effect)
            ).SetTarget(Villain)
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            intimidation_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            intimidation_boost
        )
    ]

