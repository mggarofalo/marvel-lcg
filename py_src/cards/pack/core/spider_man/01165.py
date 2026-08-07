from . import *

# Eviction Notice

def GetAbilities() -> Sequence['Ability']:

    def eviction_notice(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard_random_handcards_and_surge(targets: Sequence['CardFace']) -> None:
            player.DiscardRandomHandCards(1, effect)
            this.GainSurge(1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Peter Parker → remove Eviction Notice from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard 1 card at random from your hand. This card gains surge. Discard this obligation",
                discard_random_handcards_and_surge
            )
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            eviction_notice
        )
    ]
