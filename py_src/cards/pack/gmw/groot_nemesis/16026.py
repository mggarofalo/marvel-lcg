from . import *

# Blazing Inferno

def GetAbilities() -> Sequence['Ability']:

    def blazing_inferno(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            this.DealIndirectDamage(player.GetIdentity(), 2, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AfterPhaseBegin(
            AbilityType.ForcedResponse,
            "Villain",
            blazing_inferno
        ),
    ]

