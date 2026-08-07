from . import *

# Regenerative Healing

def GetAbilities() -> Sequence['Ability']:

    def regenerative_healing(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def heal(targets: Sequence['CardFace']):
            this.HealthUnits(targets, 4, effect)

        def discard(targets: Sequence['CardFace']):
            hero = targets[0].CastTo(Identity)
            hero.DiscardStunned(effect, rule="All")
            hero.DiscardConfused(effect, rule="All")

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 4 damage from your identity",
                heal,
            ).SetTarget("YourIdentity", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Discard each stunned and confused status card from your identity",
                discard,
            ).SetTarget("YourIdentity", has_status=["Stunned", "Confused"])
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            regenerative_healing,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().CanHeath() or \
                    effect.GetInitiator().GetIdentity().IsStunned() or \
                    effect.GetInitiator().GetIdentity().IsConfused(),
            ]
        ).SetPlay()
        .SetTarget2("YourIdentity", canbe_heal=True)
        .SetTarget2("YourIdentity", has_status=["Stunned", "Confused"])
    ]

