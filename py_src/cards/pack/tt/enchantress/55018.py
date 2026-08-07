from . import *

# Enthralled Brute

def GetAbilities() -> Sequence['Ability']:

    def enthralled_brute(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)

        if this.engaged_player:
            if this.GetEngagedPlayer().GetIdentity().HasTrait("ENTHRALLED"):
                return 1
        return 0

    def enthralled_brute_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "either place 1 charm counter on the [[Enchantment]] card in your play area",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'charm', effect)
            ).SetTarget(trait="ENCHANTMENT", from_where=["YourPlayArea"]),
            AbilityFactory.ForChoiceAbility(
                "exhaust your identity",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YourIdentity", canbe_exhaust=True)
        )

    return [
        AbilityFactory.ThisGainKeyword(
            enthralled_brute,
            health=3,
            change_on_event=OnEvent.EngagedPlayer("This")
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            enthralled_brute_boost
        ),
    ]

