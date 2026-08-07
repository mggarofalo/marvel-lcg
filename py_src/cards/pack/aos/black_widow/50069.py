from . import *

# Grappling Hook

def GetAbilities() -> Sequence['Ability']:

    def grappling_hook(effect: 'Effect', message: 'Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.CancelEffects(effect, discard_it=True)
        Faces.DiscardAll([this], effect)

    def grappling_hook_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            player.DiscardHandCards((1, 1), effect, card_type=Event)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Black Widow")
        ),
        AbilityFactory.WhenPlayerWouldPlayCard(
            AbilityType.ForcedInterrupt,
            "AnyPlayer",
            Event,
            grappling_hook
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            grappling_hook_preparation
        ),
    ]

