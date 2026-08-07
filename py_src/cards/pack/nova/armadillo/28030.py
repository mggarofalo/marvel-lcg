from . import *

# Rollin', Rollin'

def GetAbilities() -> Sequence['Ability']:

    def rollin_rollin(effect: 'Effect', player: 'Player') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        armadillo = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Armadillo"
        )
        if armadillo:
            armadillo.PutIntoPlay(player, effect)
            this.AttachTo2(armadillo, effect)


    def rollin_rollin_condition(effect: 'Effect', message: 'Message.CheckIfUnitCanDefendAgainstAttack') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        return message.attacker.IsTough()


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Armadillo"),
            if_cannot_operation=rollin_rollin
        ),
        *AbilityFactory.UnitCannotDefend(
            "Character",
            "AttachedEnemy",
            cannot_trigger_defense_ability=True,
            conditions=[rollin_rollin_condition],
        ),
    ]

