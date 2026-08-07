from . import *

# Enthralled Lackey

def GetAbilities() -> Sequence['Ability']:

    def enthralled(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            PlaceCharmCounter(player, 1, effect)

    def enthralled_lackey_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "place 1 charm counter on the [[Enchantment]] card in your play area",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'charm', effect)
            ).SetTarget(trait="ENCHANTMENT", from_where=["YourPlayArea"]),
            AbilityFactory.ForChoiceAbility(
                "confuse your identity",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget("YourIdentity", canbe_confused=True)
        )


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            enthralled
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            enthralled_lackey_boost
        ),
    ]

