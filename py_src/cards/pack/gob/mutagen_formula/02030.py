from . import *

# I See You

def GetAbilities() -> Sequence['Ability']:

    def i_see_you(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        if villain:
            if player.IsAlterEgo():
                villain.DoAttackYou(player, effect, property=AttackProperty(do_not_give_boost=True))
            else:
                villain.DoAttackYou(player, effect)

    def i_see_you_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        message.GainBoostIcon(+1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            i_see_you
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            i_see_you_boost,
            conditions=[
                lambda effect, message:
                    message.GetToPlayer().engaged_minions.FindCard(trait="GOBLIN", card_type=Minion) != None
            ],
        )
    ]

