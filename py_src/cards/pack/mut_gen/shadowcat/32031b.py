from . import *

# Phased

def GetAbilities() -> Sequence['Ability']:

    def phased_cannot_take_damage(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        # We can't trigger an effect when it register for the same event
        # e.g. play "27014"
        if isinstance(message.by_effect.bind_message, Message. WhenUnitWouldTakeDamage):
            message.by_effect.bind_message.PreventDamage("All", effect)
        else:
            this.effect.RegisterTemp(
                AbilityFactory.UnitCannotTakeDamageWhile(
                    AbilityType.Temp0,
                    message.defender,
                ),
                unregister_after_exec=False,
                until_event_end=message
            )

    return [
        AbilityFactory.WhileUnitIsDefending(
            AbilityType.NonKeyword,
            CardFinder(name="Shadowcat"),
            phased_cannot_take_damage,
        ),
        AbilityFactory.AfterUnitAttackOrDefend(
            AbilityType.ForcedResponse,
            "You",
            lambda effect, message:
                FlipSolidPhased(effect, message, "Solid"),
        ),
    ]

