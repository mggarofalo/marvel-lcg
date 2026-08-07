from . import *

# Master of Magnetism

def GetAbilities() -> Sequence['Ability']:

    def master_of_magnetism_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        magneto = Worlds.FindCardOnField(
            effect,
            name="Magneto",
            card_type=Enemy
        )

        if magneto:
            face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("MAGNETIC"))
            if face:
                magneto.GiveBoostCard(face, effect)

            magneto.DoActivate(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            master_of_magnetism_revealed
        ),
    ]

