from . import *

# "Leave Us Alone!"

def GetAbilities() -> Sequence['Ability']:

    def leave_us_alone(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = message.trigger.CastTo(Villain)
        player = message.GetToPlayer()
        for face in player.GetIdentity().GetPlacedCardArea().Get():
            villain.GiveBoostCard(face, effect)


    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            Villain,
            leave_us_alone
        )
    ]

