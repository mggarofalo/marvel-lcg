from . import *

# In Cold Blood

def GetAbilities() -> Sequence['Ability']:

    def in_cold_blood_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="The Lizard",
            card_type=Minion
        )

        def action():
            ThisCardGainSurge(effect)

        if face:
            face.DoAttackYou(player, effect, you_cannot_play_event=True, if_no_attack_was_made=action)
        else:
            action()


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            in_cold_blood_revealed
        ),
    ]

