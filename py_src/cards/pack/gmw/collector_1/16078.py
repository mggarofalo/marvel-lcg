from . import *

# Stay Awhile

def GetAbilities() -> Sequence['Ability']:

    def stay_awhile_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("RR")
            ),
            AbilityFactory.ForChoiceAbility(
                "Put the top card of your deck faceup into The Collection",
                lambda targets:
                    PutTopCardIntoTheCollection(player, effect)
            )
        )

    def stay_awhile_reveal_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        collector = Worlds.FindVillain(effect)
        player = message.GetToPlayer()
        if collector:
            def action(units: List[Unit2]):
                units = Filter.ByFinder(units, CardFinder(check_face_fn=lambda face: face == player.GetIdentity()))
                if units:
                    PutTopCardIntoTheCollection(player, effect)

            collector.DoAttackYou(player, effect, property=AttackProperty(additional_value=1), if_characters_damaged_by_this=action)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            stay_awhile_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            stay_awhile_reveal_hero
        ),
    ]

