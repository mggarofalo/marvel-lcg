from . import *

# Symbiote Smackdown

def GetAbilities() -> Sequence['Ability']:

    def symbiote_smackdown(effect: 'Effect', message: 'Message.WhenGameWouldBegin') -> None:
        this = effect.this.CastTo(Challenge)
        Unused(this)

        def action(player: 'Player'):
            face = CardFactory.GenerateCard("27191", player.player_deck, effect.world)
            face.face.PutIntoPlay(player, effect, under_control=True)

            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=False,
                name="Enraged Symbiote",
            )
            if face:
                face.Reveal(player, effect)

        Players.ForEachPlayer(effect, action)

    def enraged_symbiote_attack(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        # minion = message.attacker
        # target = message.targets

        villain = Worlds.FindVillain(effect)
        player = message.attacker.GetControlBy()
        if villain and Player.IsType(player):
            Faces.ResolveAbility([villain], player, AbilityType.ForcedResponse, effect, message)

    return [
        AbilityFactory.UsingOnlyHeroes(
            ["Venom", "Web-Warrior"],
            title_has="Spider"
        ),
        AbilityFactory.AfterGameSetup(
            symbiote_smackdown
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Symbiote Suit"),
            permanent=True,
        ),
        AbilityFactory.AfterUnitBeAttacked(
            AbilityType.Challenge,
            CardFinder(name="Enraged Symbiote"),
            enraged_symbiote_attack,
        )
    ]

