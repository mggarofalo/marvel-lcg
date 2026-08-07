from cards.pack import *

def FindTheChampion(effect: 'Effect') -> Environment|None:
    champion = Worlds.FindCardOnField(
        effect,
        name="The Champion",
        card_type=Environment
    )
    return champion

def FindTheChallengers(effect: 'Effect') -> Environment|None:
    challengers = Worlds.FindCardOnField(
        effect,
        name="The Challengers",
        card_type=Environment
    )
    return challengers


def MagGog(level: int) -> Ability:
    def magog(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        champion = FindTheChampion(effect)
        if champion:
            Faces.PlaceCountersOn([champion], level, 'ratings', effect)

    return AbilityFactory.AfterUnitAttackAndDamageUnit(
        AbilityType.ForcedResponse,
        "This",
        None,
        magog,
    )

def MagGogDefeated(value: Literal["2*", "3*"]) -> Ability:
    def magog_defeated(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.SetBeInstead(effect)
        this.ResetHealth(effect, num="10*")

        challengers = FindTheChallengers(effect)
        if challengers:
            Faces.PlaceCountersOn([challengers], value, 'ratings', effect)

        if not Worlds.IsGameOver(effect):
            def action(player: 'Player'):
                player.DealEncounterCards(1, effect)

            Players.ForEachPlayer(effect, action)


    return AbilityFactory.WhenUnitWouldBeDefeated(
        AbilityType.ForcedInterrupt,
        "This",
        magog_defeated
    )

