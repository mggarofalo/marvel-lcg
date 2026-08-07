from . import *

# * The Poison

def GetAbilities() -> Sequence['Ability']:

    def the_poison(effect: 'Effect', message: 'Message.WhenPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'poison', effect)
        identity = message.GetToPlayer().GetIdentity()
        damage = this.GetCounters('poison')
        identity.TakeDamage(this, damage, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.WhenPlayerTurnBegin(
            AbilityType.ForcedInterrupt,
            "AttachedPlayer",
            the_poison
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("3", different_type=True))
        .AnyPlayerCanDoThis(),
    ]

