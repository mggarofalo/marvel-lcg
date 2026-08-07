from . import *

# Déjà Vu

def GetAbilities() -> Sequence['Ability']:

    def deja_vu_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 1 damage",
                lambda targets:
                    player.GetIdentity().TakeDamage(this, 1, effect),
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Place 1 threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 1, effect)
            ).SetTarget(MainScheme, can_place_threat=True)
        )

        any_player = Worlds.FindPlayer(None, effect)
        if any_player:
            Faces.ShuffleAllTo([this], any_player.player_deck, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            deja_vu_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        )
    ]

