from . import *

# Brotherhood Beatdown

def GetAbilities() -> Sequence['Ability']:

    def brotherhood_beatdown_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        avalanche = Worlds.FindCardOnField(
            effect,
            name="Avalanche",
            card_type=Enemy
        )
        if avalanche:
            Faces.ExhaustAll([identity], effect)
        blob = Worlds.FindCardOnField(
            effect,
            name="Blob",
            card_type=Enemy
        )
        if blob:
            Faces.GiveStatus([identity], "Stunned", effect)
        pyro = Worlds.FindCardOnField(
            effect,
            name="Pyro",
            card_type=Enemy
        )
        if pyro:
            identity.TakeIndirectDamage(this, 2, effect)
        toad = Worlds.FindCardOnField(
            effect,
            name="Toad",
            card_type=Enemy
        )
        if toad:
            player.DiscardRandomHandCards(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            brotherhood_beatdown_revealed
        ),
    ]

