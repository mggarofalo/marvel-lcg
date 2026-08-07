from . import *

# Fallen Warrior

def GetAbilities() -> Sequence['Ability']:

    def fallen_warrior(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        ally = player.DiscardDeckUntil(
            effect,
            card_type=Ally,
        )

        if ally:
            def action():
                this.AttachTo2(ally, effect)
            this.effect.RegisterTemp(
                AbilityFactory.WhenCardEnterPlay(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        action(),
                    conditions=[
                        lambda eneter_effect, eneter_message:
                            ally == eneter_message.trigger
                    ]
                ),
                unregister_after_exec=True,
                until_turn_end=True
            )
            ally.PutIntoPlay(player, effect)

    return [
        AbilityFactory.TreatAttachedCardAsMinion(
            Ally,
            "Undead Minion",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            fallen_warrior
        ),
    ]

