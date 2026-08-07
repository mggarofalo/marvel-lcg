from . import *

# Magnets and Metal

def GetAbilities() -> Sequence['Ability']:

    def magnets_and_metal(effect: 'Effect', message: 'Message.WhenGameWouldBegin'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)
        face = Search.EncounterCard(
            effect,
            include_discard_pile=False,
            name="Seized!",
            card_type=SchemeSide2
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.UsingOnlyHeroes(
            ["Iron Man", "War Machine", "Ironheart", "SP//dr"]
        ),
        AbilityFactory.AfterGameSetup(
            magnets_and_metal,
        )
    ]

