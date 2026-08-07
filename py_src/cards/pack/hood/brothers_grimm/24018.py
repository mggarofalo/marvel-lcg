from . import *

# * Brothers Grimm

def GetAbilities() -> Sequence['Ability']:

    def brothers_grimm(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
        )
        if face:
            face.Reveal(player, effect)


    def brothers_grimm_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action():
            first_player = Worlds.GetFirstPlayer(effect)
            this.PutIntoPlay(first_player, effect)
        message.AfterThisActivation(effect, action)


    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            brothers_grimm
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            brothers_grimm_boost
        ),
    ]

