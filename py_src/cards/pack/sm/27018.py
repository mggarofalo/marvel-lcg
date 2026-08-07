from . import *

# Across the Spider-Verse

def GetAbilities() -> Sequence['Ability']:

    def across_the_spider_verse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        def this_ability(player: 'Player'):
            face = Search.PlayerCard(
                effect,
                player,
                include_player_deck=False,
                include_discard_pile=True,
                card_type=Ally,
                trait="WEB-WARRIOR"
            )

            def choose_player(faces: Sequence['CardFace']):
                for face in faces:
                    chose_player = face.GetControlByPlayer()

                    chose_player.MayChooseOneAbility(
                        effect,
                        AbilityFactory.ForChoiceAbilityWithCost(
                            Cost("3"),
                            "Spend 3 resources of any type to repeat this ability",
                            lambda targets, res:
                                this_ability(chose_player)
                        )
                        .SetCostFunc(CostFunc.Exhaust(trait="WEB-WARRIOR", from_where=["YouControlCards"])),
                    )

            if face:
                face.PutIntoPlay(player, effect)

                player.ChooseAbilities(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Choose a player. That player may spend 3 resources of any type to repeat this ability",
                        lambda targets: choose_player(targets),
                        # condition=has_web_warrior(effect),
                    ).SetTarget("Players")
                )

        initiator = effect.GetInitiator()
        this_ability(initiator)

    # def has_web_warrior(effect: 'Effect'):
    #     initiator = effect.GetInitiator()
    #     return [x for x in initiator.discard_pile.Get(True) if Ally.IsType(x) and x.HasTrait('WEB-WARRIOR')] != []

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            across_the_spider_verse,
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(
                trait="WEB-WARRIOR",
                from_where=["YouControlCards"])
        ),
    ]

