from . import *

# * Major Domo

def GetAbilities() -> Sequence['Ability']:

    def major_domo(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        num = message.total_dealt_damage
        Worlds.DiscardEncounterCards(num, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Mojo")
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YBR")),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Mojo"),
            major_domo
        ),
    ]

