from . import *

# Consume the World

def GetAbilities() -> Sequence['Ability']:

    def consume_the_world(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        effect.world.SetGameOver(False, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(Scheme2).threat == 0,
            amplify=-1,
            change_on_event=OnEvent.Threat("This")
        ),
        AbilityFactory.AfterSchemePlaceThreat(
            AbilityType.ForcedResponse,
            "This",
            consume_the_world,
            has_at_least_threat=12,
        ),
    ]

