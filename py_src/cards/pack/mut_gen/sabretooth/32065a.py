from . import *

# Find the Senator

def GetAbilities() -> Sequence['Ability']:


    def find_the_senator(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        ally = Worlds.FindCardOnField(
            effect,
            ROBERT_KELLY_FINDER,
            card_type=Ally
        )
        if ally:
            ally.card.SetOwner(player)
            Faces.MoveAllTo([ally], player.allies, effect)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            scheme.Advance("2A", effect)

        this.card.Flip(effect)


    return [
        *RobertKellyCannotBeHealedByPlayerCardEffectsAndCannotHaveUpgradesAttached(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            find_the_senator,
        ),
    ]

