from . import *

# * Nick Fury

def GetAbilities() -> Sequence['Ability']:

    def nick_fury(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        def remove_2_threat_from_a_scheme(targets: Sequence['CardFace']) -> None:
            this.RemoveThreatFromSchemes(targets, 2, effect)

        def draw_3_cards(targets: Sequence['CardFace']) -> None:
            initiator.DrawUp(3, effect)

        def deal_4_damage_to_an_enemy(targets: Sequence['CardFace']) -> None:
            this.DealDamage(targets, 4, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove 2 threat from a scheme",
                remove_2_threat_from_a_scheme
            ).SetTarget(Scheme2),
            AbilityFactory.ForChoiceAbility(
                "Draw 3 cards",
                draw_3_cards
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Deal 4 damage to an enemy",
                deal_4_damage_to_an_enemy
            ).SetTarget(Enemy),
        )


    return [
        AbilityFactory.DiscardThisWhenRoundEnd(
            AbilityType.ForcedResponse,
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            nick_fury
        )
    ]
