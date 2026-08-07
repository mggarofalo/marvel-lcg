from . import *

# Lone Wolf

def GetAbilities() -> Sequence['Ability']:

    def remove_all_allies(effect: 'Effect', message: 'Message.WhenGameBeginSetup'):
        allies: List['CardFace'] = []
        for player in Worlds.GetPlayers(effect):
            allies += player.player_deck.FindCards(card_type=Ally)
        Faces.FlipAllTo(allies, True, effect)
        Faces.RemoveAllFromGame(allies, effect)

    def lone_wolf(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit'):
        this = effect.this.CastTo(Challenge)
        Unused(this)
    
        if message.attacker:
            player = message.attacker.GetControlByPlayer()
            player.DrawUp(1, effect)

    return [
        AbilityFactory.UsingOnlyHeroes(
            solo=True
        ),
        AbilityFactory.WhenGameBeginSetup(
            remove_all_allies
        ),
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Challenge,
            Identity,
            Minion,
            lone_wolf
        )
    ]

