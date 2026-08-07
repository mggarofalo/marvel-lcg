from . import *

# Hope's Captor

def GetAbilities() -> Sequence['Ability']:

    def hopes_captor(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.DoSchemeInstead(effect)

    def hopes_captor_defeated(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        message.trigger.ResetHealth(effect)

        this.card.Flip(effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            Villain,
            hopes_captor,
            conditions=[
                lambda effect, message:
                    message.GetAgainstPlayer().engaged_minions.FindCard(trait="MARAUDER", card_type=Minion) != None
            ],
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            Villain,
            hopes_captor_defeated
        ),
    ]

