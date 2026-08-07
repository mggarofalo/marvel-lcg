from . import *

# Silk Sense Overload

def GetAbilities() -> Sequence['Ability']:

    def silk_sense_overload(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        message.ChangeToArea(this.GetPlacedCardArea(), effect)

        def action():
            size = this.GetPlacedCardArea().GetSize()
            if size == 2:
                player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Discard this card",
                        lambda targets:
                            Faces.DiscardAll(targets, effect)
                    ).SetTarget([this], canbe_discard=True)
                )
            elif size >= 3:
                player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Remove it from the game",
                        lambda targets:
                            Faces.RemoveAllFromGame(targets, effect)
                    ).SetTarget([this])
                )

        RunAt.AfterEventEnd(effect, message, action)


    return [
        AbilityFactory.WhenCardWouldBeTuckUnder(
            AbilityType.ForcedInterrupt,
            None,
            "YourIdentity",
            silk_sense_overload,
            conditions=[
                lambda effect, message:
                    PlayerCard.IsType(message.by_effect.this)
            ]
        ),
    ]

