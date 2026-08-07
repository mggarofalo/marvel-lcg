from . import *

# Hook, Line, and Sinker

def GetAbilities() -> Sequence['Ability']:

    def hook_line_and_sinker_response(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.ExhaustAll([message.trigger], effect)

    def hook_line_and_sinker(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.SetDealIndirectDamage(effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.NonKeyword,
            CardFinder2("BRUTE", Enemy),
            hook_line_and_sinker
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            CardFinder(card_type=Friend, canbe_exhaust=True),
            hook_line_and_sinker_response
        ),
    ]

