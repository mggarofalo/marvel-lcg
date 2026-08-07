from . import *

# Prelate Armor

def GetAbilities() -> Sequence['Ability']:

    def prelate_armor(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus([message.trigger], "Tough", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Unus")
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Unus"),
            prelate_armor,
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Unus"),
        ).SetCost(Cost("BR")),
    ]

