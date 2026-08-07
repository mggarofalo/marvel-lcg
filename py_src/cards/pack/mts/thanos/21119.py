from . import *

# Master of the Stones

def GetAbilities() -> Sequence['Ability']:

    def master_of_the_stones(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        face = GetInfinityStone(effect).GetTopCard()
        if face:
            face.PutIntoPlay(player, effect)

        def action():
            Faces.DiscardAll([this], effect)

        RunAt.AfterEnemyActivationEnd(effect, message.would_message, action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Thanos")
        ),
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Thanos"),
            master_of_the_stones
        ),
    ]

