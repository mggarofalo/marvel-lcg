from . import *

# Negative Zone Prison

def GetAbilities() -> Sequence['Ability']:

    def negative_zone_prison(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.PlaceCardHere(message.trigger, False, effect)


    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.CastTo(MainScheme).GetPlacedCardArea().GetSize(),
            target_threat=-1,
            change_on_event=OnEvent.TuckUnder("Here")
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            Ally,
            negative_zone_prison,
            attacker=Enemy,
        ),
    ]

