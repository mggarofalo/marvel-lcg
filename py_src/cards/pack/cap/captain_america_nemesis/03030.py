from . import *

# Hail Hydra!

def GetAbilities() -> Sequence['Ability']:

    def hail_hydra(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player') -> List['Player']:
            has_attack = False
            if player.IsHero():
                for unit in player.GetEngagedMinions():
                    if unit.HasTrait("HYDRA"):
                        unit.DoAttackYou(player, effect)
                        has_attack = True
            if not has_attack:
                return [player]
            return []

        need_search_card_players: List['Player'] = Players.ForEachPlayer(effect, action, [])

        for player in need_search_card_players:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="HYDRA",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hail_hydra
        )
    ]
