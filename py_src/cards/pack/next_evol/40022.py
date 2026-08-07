from . import *

# Uncanny X-Force

def GetAbilities() -> Sequence['Ability']:

    def uncanny_x_force(effect: 'Effect', message: 'Message.AfterUnitThwartScheme') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        message.trigger.GainForThisActive(
            effect,
            message.would_thw_message,
            thwart_consequential_damage=-1
        )

    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        AbilityFactory.AfterUnitThwartScheme(
            AbilityType.NonKeyword,
            "YouControlAlly",
            SchemeSide2,
            uncanny_x_force,
            conditions=[
                lambda effect, message:
                    Faces.EachOfAre(effect.GetInitiator().GetControlCharacters(), CardFinder2("X-FORCE"))
            ]
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Ally,
            control_by="You",
            if_each_of_your_characters_has_trait="X-FORCE",
            thwart=1,
            change_on_event=OnEvent.Trait("YouControlCharacter")
        ),
    ]

