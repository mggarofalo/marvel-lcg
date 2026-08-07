from . import *

# Telekinetic Dragon

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_dragon_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        value = FacesCounter.GetPrintedResourcesCount(player.GetControlCards(), "B")
        if value == 0:
            ThisCardGainSurge(effect)
        else:
            identity.TakeIndirectDamage(this, value, effect)

    def telekinetic_dragon_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("B")
            ),
            AbilityFactory.ForChoiceAbility(
                "Confuse your identity",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget(
                [identity],
                finder=CardFinder(canbe_confused=True))
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            telekinetic_dragon_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            telekinetic_dragon_boost
        ),
    ]

