from . import *

# * Tigra

def GetAbilities() -> Sequence['Ability']:

    def tigra(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        size = len(initiator.GetEngagedMinions())
        size = min(size, 3)
        initiator.DrawUp(size, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(card_type=Minion, with_attach=CardFinder(name="Hunted")),
            guard=-1,
            change_on_event=OnEvent.AttachedMove(Upgrade)
        ),
        AbilityFactory.AfterPlayerPhaseBegin(
            AbilityType.Response,
            tigra,
            conditions=[
                lambda effect, message:
                    len(effect.GetInitiator().GetEngagedMinions()) > 0
            ]
        ).SetName("On the Hunt"),
    ]

