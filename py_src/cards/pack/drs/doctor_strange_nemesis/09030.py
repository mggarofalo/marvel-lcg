from . import *

# Counterspell

def GetAbilities() -> Sequence['Ability']:

    def counterspell(effect: 'Effect', message: 'Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.CancelEffects(effect, discard_it=True)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourHero",
        ),
        AbilityFactory.WhenPlayerWouldPlayCard(
            AbilityType.ForcedInterrupt,
            "AttachedPlayer",
            Event,
            counterspell,
            conditions=[
                lambda effect, message:
                    not message.be_cancel
            ],
        )
    ]

