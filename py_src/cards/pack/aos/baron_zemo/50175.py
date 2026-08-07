from . import *

# Battle of Wits

def GetAbilities() -> Sequence['Ability']:

    def battle_of_wits_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        enemy = Worlds.FindCardOnField(
            effect,
            card_type=Enemy,
            name="Baron Zemo"
        )
        if enemy:

            def action(place_effect: 'Effect', place_message: 'Message.WhenSchemeWouldPlaceThreat'):

                def prevent(targets: Sequence['CardFace'], res: 'Resources'):
                    size = res.val
                    place_message.PreventThreat(size, effect)
                    RemoveSecretCountersFromAmongBoardMemberEnvironments(size, effect)

                player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbilityWithCost(
                        Cost.FromText("B"*place_message.value, up_to=True),
                        None,
                        prevent
                    )
                )

            temp_effects = this.effect.RegisterTemp(
                AbilityFactory.WhenSchemeWouldPlaceThreat(
                    AbilityType.Temp0,
                    None,
                    action,
                ),
                unregister_after_exec=False
            )

            enemy.DoSchemes(player, effect)

            Effects.UnRegister(temp_effects)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            battle_of_wits_revealed
        ),
    ]

