from . import *

# Uncontrollable Power

def GetAbilities() -> Sequence['Ability']:

    def uncontrollable_power(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            player.DiscardHandCards((0, 1), effect)

            value = FacesCounter.GetMostCommonType(player.hand_cards.Get())
            player.GetIdentity().PlaceThreatOnSchemes([this], value, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            uncontrollable_power
        ),
    ]
