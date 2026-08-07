from . import *

# Parental Guidance

def GetAbilities() -> Sequence['Ability']:

    def parental_guidance_in_play(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        upgrade = FindGeorgeStacy(effect)
        if upgrade:
            upgrade.PlaceCardHere(effect.targets, False, effect)


    def parental_guidance_not_in_play(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        upgrade = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="George Stacy",
            card_type=Support
        )
        if upgrade:
            initiator.GainCard(upgrade, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            parental_guidance_not_in_play,
            conditions=[
                lambda effect, message:
                    FindGeorgeStacy(effect) == None
            ]
        ).SetPlay().SetLabel(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            parental_guidance_in_play,
            conditions=[
                lambda effect, message:
                    FindGeorgeStacy(effect) != None
            ]
        ).SetPlay().SetLabel()
        .SetTarget(Event, from_where=["YourHandCards", "YourDiscardPile"]),
    ]

