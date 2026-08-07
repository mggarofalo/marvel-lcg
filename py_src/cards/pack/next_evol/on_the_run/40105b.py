from . import *

# Hope's Captor

def GetAbilities() -> Sequence['Ability']:

    def hopes_captor_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            scheme.Advance("2A", effect)

    def hopes_captor(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.DoSchemeInstead(effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            health="6*",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            hopes_captor_revealed
        ).SetCannotBeCancel(),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            Villain,
            hopes_captor,
            conditions=[
                lambda effect, message:
                    message.GetAgainstPlayer().engaged_minions.FindCard(trait="MARAUDER", card_type=Minion) != None
            ],
        ),
    ]

