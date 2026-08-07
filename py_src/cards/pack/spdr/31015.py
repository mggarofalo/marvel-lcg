from . import *

# * Spider-Man Noir

def GetAbilities() -> Sequence['Ability']:

    def spider_man_noir(effect: 'Effect', message: 'Message.AfterCardRevealed') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.PlaceCardHere(message.trigger, False, effect)

    return [
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui:
                effect.this.GetPlacedCardArea().GetSize(),
            attack=1,
            thwart=1,
            change_on_event=OnEvent.PlacedCard("Here"),
        ),
        AbilityFactory.AfterYouResolveTreachery(
            AbilityType.Response,
            spider_man_noir,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().IsControl(CardFinder(trait="WEB-WARRIOR", check_face_fn=lambda face: face!=effect.this)) and \
                    effect.this.GetPlacedCardArea().GetSize() < 3,
            ]
        ).SetTarget("This"),
    ]

