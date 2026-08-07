from . import *

# S.H.I.E.L.D. Agent

def GetAbilities() -> Sequence['Ability']:

    def shield_agent(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou|Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        PlaceSecretCounterOnBoardMemberCard(Environment, 1, effect, with_fewest_secret=True)


    return [
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            shield_agent
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            shield_agent
        ),
    ]

