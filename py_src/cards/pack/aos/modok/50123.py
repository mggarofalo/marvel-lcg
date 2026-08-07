from . import *

# "It's Alive!"

def GetAbilities() -> Sequence['Ability']:

    def its_alive_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        engaged_player: List[Player] = []

        def mark_engaged(check_effect: 'Effect', check_message: 'Message.WhenMinionEngagePlayer'):
            nonlocal engaged_player
            if effect == check_message.by_effect:
                engaged_player.append(check_message.engaged_player)

        effects = this.effect.RegisterTemp(
            AbilityFactory.WhenMinionEngagePlayer(
                AbilityType.Temp0,
                None,
                None,
                mark_engaged,
            ),
            unregister_after_exec=False
        )

        def action(player: 'Player'):
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                card_type=Minion,
                trait="ADAPTOID",
            )
            if face:
                face.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)

        def action2(player: 'Player'):
            if player not in engaged_player:
                player.DealEncounterCards(1, effect)

        Players.ForEachPlayer(effect, action2)

        Effects.UnRegister(effects)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            its_alive_revealed
        ),
    ]

