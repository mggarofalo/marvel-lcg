from cards.pack import *

def CitizenV(value: int) -> List['Ability']:

    def citizen_v(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.SetBeInstead(effect)

        this.HealthUnits([this], value, effect)

    return [
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeyword,
            "This",
            unless_card_in_victory_display=(
                "1*",
                CardFinder2("THUNDERBOLT", Minion)
            ),
        ),
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.ForcedInterrupt,
            "This",
            citizen_v,
            # against_you=True,
            during_villain_step=2,
            conditions=[
                lambda effect, message:
                    message.GetToPlayer().GetEngagedMinions(CardFinder2("THUNDERBOLT")) != []
            ]
        ),
    ]

def RevealRandomSetasideThunderboltMinion(player: 'Player', effect: 'Effect'):
    faces = Worlds.AsideDeck(effect).FindCards(trait="THUNDERBOLT", card_type=Minion)
    face = Rand.RandomChoice(faces, effect)
    face.Reveal(player, effect)
    return face

def GetRemainingSetasideThunderboltMinion(effect: 'Effect'):
    faces = Worlds.AsideDeck(effect).FindCards(trait="THUNDERBOLT", card_type=Minion)
    return faces

def EachPlayerEngagesEachMinionEngagedWithThePlayerClockwiseFromThem(effect: 'Effect'):
    minions: Dict[int, List[Minion]] = {}

    for player in Worlds.GetPlayers(effect):
        player_id = player.player_id
        minions[player_id] = player.GetEngagedMinions()

    def action(player: 'Player'):
        player_id = player.player_id + 1
        if player_id >= len(Worlds.GetPlayers(effect)):
            player_id = 0
        for minion in minions[player_id]:
            minion.EngagePlayer(player, effect)

    Players.ForEachPlayer(effect, action)

