from . import *

# * Hope Summers

def GetAbilities() -> Sequence['Ability']:

    def hope_summers(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="SUPERPOWER",
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "This",
            get_new_value=lambda effect, message:
                effect.this.GetControlByPlayer().GetIdentity().traits if \
                    effect.this.GetControlByOrOwner().IsPlayer() else [],
            trait=1,
            change_on_event=OnEvent.Trait("YourIdentity")
        ),
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            hope_summers,
        ),
    ]

