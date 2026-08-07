from . import *

# * Captain America

def GetAbilities() -> Sequence['Ability']:

    def captain_america_setup(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        Find.FindAndAttachTo(
            effect,
            this,
            who_perform=team,
            name="Cap's Shield",
        )

    def captain_america(effect: 'Effect', message: 'Message.AfterCardAttachTo') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            captain_america_setup
        ),
        AbilityFactory.AfterCardAttachTo(
            AbilityType.ForcedResponse,
            CardFinder(name="Cap's Shield"),
            "This",
            captain_america
        ),
    ]

