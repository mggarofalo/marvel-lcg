from . import *

# Hall of Nastrond

def GetAbilities() -> Sequence['Ability']:

    def hall_of_nastrond(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            odin = scheme.components.inventory.GetDeck().FindCard(name="Odin", card_type=Ally)
        else:
            odin = None

        if odin:
            odin.PutIntoPlay(first_player, effect)

        def action(player: 'Player'):
            player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hall_of_nastrond
        ),
    ]

