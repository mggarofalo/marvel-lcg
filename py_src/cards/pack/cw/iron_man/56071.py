from . import *

# Supersonic Punch

def GetAbilities() -> Sequence['Ability']:

    def supersonic_punch_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        iron_man = Worlds.FindCardOnField(
            effect,
            name="Iron Man",
            card_type=Enemy
        )

        if iron_man:
            iron_man.DoSchemes(player, effect)
            if iron_man.GetInventoryDeck().FindCard(CardFinder2("TECH", Attachment)):
                Faces.GiveStatus([identity], "Confused", effect)

    def supersonic_punch_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        iron_man = Worlds.FindCardOnField(
            effect,
            name="Iron Man",
            card_type=Enemy
        )

        if iron_man:
            if iron_man.GetInventoryDeck().FindCard(trait="TECH", card_type=Attachment):
                piercing = True
            else:
                piercing = False
            iron_man.DoAttackYou(player, effect, property=AttackProperty(piercing=piercing))


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            supersonic_punch_revealed
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            supersonic_punch_hero
        ),
    ]

