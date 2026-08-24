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
    # Seating order comes from the players still in the game, not from a count.
    # `Worlds.GetPlayers` drops a player whose hero area is empty, so once anyone
    # is knocked out the surviving `player_id`s stop being 0..n-1 -- and wrapping
    # on `len(players)` back to a literal 0 then reads a seat nobody holds.
    seats = Worlds.GetPlayers(effect)
    order = [player.player_id for player in seats]

    minions: Dict[int, List[Minion]] = {}
    for player in seats:
        minions[player.player_id] = player.GetEngagedMinions()

    def action(player: 'Player'):
        clockwise = order[(order.index(player.player_id) + 1) % len(order)]
        for minion in minions[clockwise]:
            minion.EngagePlayer(player, effect)

    Players.ForEachPlayer(effect, action)

