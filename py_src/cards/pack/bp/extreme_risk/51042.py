from . import *

# Extreme Risk

def GetAbilities() -> Sequence['Ability']:

    def extreme_risk_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Find.FindAndReveal(
            effect,
            player,
            name="Joystick",
            card_type=Enemy
        )
        def action():
            ThisCardGainSurge(effect)

        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    def extreme_risk_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        give_effect = player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give the activating enemy an additional boost card",
                lambda targets:
                    message.GiveActivatingEnemyAdditionalBoostCard(1, effect)
            ),
        )

        if give_effect:
            player.DrawUp(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            extreme_risk_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            extreme_risk_boost
        ),
    ]

