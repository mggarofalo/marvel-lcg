from . import *

# Clint Barton

def GetAbilities() -> Sequence['Ability']:

    def weapon_of_choice(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name=HAWKEYE_BOW_NAME,
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            weapon_of_choice,
            conditions=[
                lambda effect, message:
                    not FindOnPlayHawkeyesBow(effect.GetInitiator()),
            ]
        ).SetName("Weapon of Choice")
        .SetCost(Cost("1"))
        .LimitOncePerPhase()
    ]
