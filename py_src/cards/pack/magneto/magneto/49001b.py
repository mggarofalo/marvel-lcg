from . import *

# * Erik Lehnsherr

def GetAbilities() -> Sequence['Ability']:

    def erik_lehnsherr(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        # faces = player.discard_pile.GetTops(3)
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            erik_lehnsherr,
            to_this_form=True,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().discard_pile.GetSize() > 0
            ],
        ).SetName("Survivor")
        .SetTarget(range=(3, 3), from_where=["YourDiscardPileTop"]),
    ]

