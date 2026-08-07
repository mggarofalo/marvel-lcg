from . import *

# Ruler of the Astral Plane

def GetAbilities() -> Sequence['Ability']:

    def ruler_of_the_astral_plane_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        if player.GetEngagedMinions(CardFinder2("CONTROLLED")):
            this.Reveal(player, effect)

    def ruler_of_the_astral_plane(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        faces = Worlds.FindCardsOnField(
            effect,
            name="Possessed",
        )

        player.AskDiscardFace(faces, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            ruler_of_the_astral_plane,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ruler_of_the_astral_plane_boost
        ),
    ]

