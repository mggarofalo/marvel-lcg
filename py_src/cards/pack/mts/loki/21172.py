from . import *

# * Loki's Cape

def GetAbilities() -> Sequence['Ability']:

    def lokis_cape(effect: 'Effect', message: 'Message.WhenScenarioEvent') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Loki")
        ),
        AbilityFactory.WhenScenarioEvent(
            AbilityType.ForcedResponse,
            "SwapLokiWithRandomSetAsideLokiVillain",
            lokis_cape
        ),
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "You",
            DiscardThisCard,
            against_who=CardFinder(name="Loki")
        ).SetCost(Cost("YB")),
    ]

