from . import *

# * Blizzard

def GetAbilities() -> Sequence['Ability']:

    def blizzard(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        if not message.attacked.IsInPlay():
            return

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Encased in Ice",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(message.attacked, effect)
        else:
            Faces.GiveStatus([message.attacked], "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "Character",
            blizzard
        ),
    ]

