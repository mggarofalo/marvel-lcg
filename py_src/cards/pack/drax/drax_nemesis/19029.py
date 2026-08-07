from . import *

# "I Will Destroy You!"

def GetAbilities() -> Sequence['Ability']:

    def i_will_destroy_you_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minion = Worlds.FindCardOnField(
            effect,
            name="Yotat the Destroyer",
            card_type=Minion
        )

        def action():
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.DoAttackYou(player, effect)
        if minion:
            minion.DoAttackYou(player, effect, property=AttackProperty(additional_value=1),
                if_no_attack_was_made=action
            )
        else:
            action()

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            i_will_destroy_you_hero
        )
    ]

