from . import *

# Cybermods

def GetAbilities() -> Sequence['Ability']:

    def cybermods(effect: 'Effect', player: 'Player') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(player, effect)
            this.AttachTo2(face, effect)

    def cybermods_interrupt(effect: 'Effect', message: 'Message.WhenCardWouldDiscard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.ShuffleAllTo([message.trigger], "EncounterDeck", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Lady Deathstrike"),
            if_cannot_operation=cybermods
        ),
        AbilityFactory.WhenCardWouldDiscard(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            cybermods_interrupt
        ),
    ]

