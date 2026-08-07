from . import *

# * Bobby Drake

def GetAbilities() -> Sequence['Ability']:

    def bobby_drake(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

    return [
        AbilityFactory.BeginGameWithSetAside([
            "46002",
            "46002",
            "46002",
            "46002",
            "46002",
            "46002"
        ]),
        # Bobby Drake begins the game with 6 Frostbite upgrades set aside.
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.Response,
            "You",
            bobby_drake,
            to_this_form=True,
        ).SetName("Cool Off")
        .SetTarget(trait="ICE", from_where=["YourDiscardPile"],
            range=(1,
                lambda effect:
                   Worlds.FindCardSizeOnField(effect, name="Frostbite"),
            ))
    ]

