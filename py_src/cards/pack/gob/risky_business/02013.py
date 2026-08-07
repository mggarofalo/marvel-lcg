from . import *

# Mad Genius

def GetAbilities() -> Sequence['Ability']:

    def mad_genius_goblin(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Green Goblin",
            card_type=Villain
        )
        assert villain, f"{effect=}"
        hero = Filter.One(Worlds.GetOnFieldHeroes(effect), effect, fewest_remaining_hp=True)
        atk_message = None
        if hero:
            atk_message = villain.BasicAttack([hero], effect)
        if atk_message == None:
            this.GainSurge(1, effect)

    def discard_deck_osborn(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        counter = CriminalEnterpriseGetInfamyCounters(effect)
        player.DiscardDeckTopCards(counter, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            VillainIsGreenGoblin,
            mad_genius_goblin
        ),
        AbilityFactory.WhenThisRevealed(
            VillainIsNormanOsborn,
            discard_deck_osborn
        )
    ]

