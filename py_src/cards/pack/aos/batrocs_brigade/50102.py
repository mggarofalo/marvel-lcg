from . import *

# Soldiers of Fortune

def GetAbilities() -> Sequence['Ability']:

    def soldiers_of_fortune_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action(targets: Sequence['CardFace']):
            Find.FindAndReveal(
                effect,
                player,
                trait="MERCENARY",
                card_type=Minion,
                if_no_entered_play=lambda: ThisCardGainSurge(effect)
            )

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("3"),
            ),
            AbilityFactory.ForChoiceAbility(
                "Find a [[Mercenary]] minion and reveal it",
                action
            )
        )

    def soldiers_of_fortune_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(targets: Sequence['CardFace']):
            this.GainBoostIcons(3, effect)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("1"),
            ),
            AbilityFactory.ForChoiceAbility(
                "This card gains [boost][boost][boost]",
                action
            )
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            soldiers_of_fortune_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            soldiers_of_fortune_boost
        ),
    ]

