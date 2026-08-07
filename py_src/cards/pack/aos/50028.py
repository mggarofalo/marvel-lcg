from . import *

# Front Organization

def GetAbilities() -> Sequence['Ability']:

    def front_organization(effect: 'Effect', message: 'Message.WhenCardWouldDiscard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        AbilityFactory.WhenCardWouldDiscard(
            AbilityType.Interrupt,
            None,
            front_organization,
            conditions=[
                lambda effect, message:
                    effect.this.GetControlBy() == message.trigger.GetControlBy() and \
                    EncounterCard.IsType(message.by_effect.this)
            ]
        ),
    ]

