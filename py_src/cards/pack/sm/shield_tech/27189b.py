from . import *

# Wrist Navigators

def GetAbilities() -> Sequence['Ability']:

    def wrist_navigators(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.AttachTo2(message.trigger, effect)

    def wrist_navigators_interrupt(effect: 'Effect', message: 'Message.WhenUnitBeDefeated|Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)
        initiator.DiscardHandCards((1, 1), effect)
        this.AttachTo2(initiator.GetIdentity(), effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            CardFinder(card_type=Minion|SchemeSide2),
            wrist_navigators
        ).SetTarget("Trigger"),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "AttachedCard",
            wrist_navigators_interrupt,
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.Interrupt,
            "AttachedCard",
            wrist_navigators_interrupt,
        )
    ]

