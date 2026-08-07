from . import *

# Elaborate Trap

def GetAbilities() -> Sequence['Ability']:

    def elaborate_trap_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        schemes = Worlds.FindCardsOnField(
            effect,
            trait="TRAP!",
            card_type=SchemeSide2
        )

        if not Faces.ResolveAbility(schemes, player, AbilityType.WhenDefeated, effect):
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="TRAP!",
                card_type=SchemeSide2
            )
            if face:
                face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            elaborate_trap_revealed
        ),
    ]

