from . import *

# * Spider-Man: Miles Morales

def GetAbilities() -> Sequence['Ability']:

    def spider_man(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        text = initiator.AskChooseOneText(["THW", "ATK"])

        thwart = None
        attack = None
        if text == "THW":
            thwart = 2
        if text == "ATK":
            attack = 2

        this.GainUntilPhaseEnd(
            effect,
            attack=attack,
            thwart=thwart
        )

    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            "This",
            spider_man
        ),
    ]

