from . import *

# Superhero Registration Act

def GetAbilities() -> Sequence['Ability']:

    def superhero_registration_act(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        if Worlds.IsCompetitiveMode(effect):
            # team = Worlds.GetEnemyTeam(effect)
            assert False
        if Worlds.IsCooperativeMode(effect):
            leader = Worlds.GetEnemyLeader(effect)
            if leader:
                Find.FindAndReveal(
                    effect,
                    player,
                    finder=CardFinder(card_type=EncounterSideScheme, set_name=leader.paper.set_name)
                )


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            superhero_registration_act
        ),
    ]

