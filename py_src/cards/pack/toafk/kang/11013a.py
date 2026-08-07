from . import *

# Kang's Wrath - 4A

def GetAbilities() -> Sequence['Ability']:

    def kangs_wrath(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = message.GetToPlayer()

        kang = Worlds.AsideDeck(effect).FindCard(
            name="Kang",
            card_type=Villain,
            stage=3,
        )
        assert kang
        kang.PutIntoPlay(player, effect)
        kang.Reveal(player, effect)

        # kang.AddToGameArea()

        for face in this.GetPlacedCardArea().Get():
            assert face.IsName(KANGS_DOMINION), f"{face=}"
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            kangs_wrath
        )
    ]

