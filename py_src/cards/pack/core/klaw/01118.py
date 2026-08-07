from . import *

# * Sonic Converter

def GetAbilities() -> Sequence['Ability']:

    def sonic_converter(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        assert message.dealt_damage > 0, f"{message.dealt_damage=}"
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus([message.attacked], "Stunned", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Klaw")
        ),
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedResponse,
            CardFinder(name="Klaw"),
            "Character",
            sonic_converter,
            target_in_play=True,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR"))
    ]
