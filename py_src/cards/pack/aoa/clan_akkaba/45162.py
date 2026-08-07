from . import *

# Tyrant Worship

def GetAbilities() -> Sequence['Ability']:

    def tyrant_worship_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FindCardOnField(
            effect,
            name="Ancient Ritual",
            card_type=Scheme2
        )

        if scheme:
            this.PlaceThreatOnSchemes([scheme], 5, effect)


    def tyrant_worship_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 3 threat on Ancient Ritual",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 3, effect)
            ).SetTarget(name="Ancient Ritual"),
            AbilityFactory.ForChoiceAbility(
                "This card gains [boost][boost][boost]",
                lambda targets:
                    this.GainBoostIcons(3, effect)
            )
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tyrant_worship_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            tyrant_worship_boost
        ),
    ]

