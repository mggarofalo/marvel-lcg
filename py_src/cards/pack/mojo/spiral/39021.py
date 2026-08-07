from . import *

# Well-Armed

def GetAbilities() -> Sequence['Ability']:

    def well_armed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = Worlds.FindVillain(effect, name="Spiral")
        if villain:
            if villain.GetInventoryDeck().FindCard(name="Spiral's Swords"):
                villain.DoAttackYou(player, effect)
            else:
                face = Search.EncounterCard(
                    effect,
                    include_discard_pile=True,
                    name="Spiral's Swords",
                    card_type=Attachment
                )
                if face:
                    face.AttachTo2(villain, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            well_armed_revealed
        ),
    ]

