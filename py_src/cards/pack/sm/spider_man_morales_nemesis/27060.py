from . import *

# Slice and Dice

def GetAbilities() -> Sequence['Ability']:

    def slice_and_dice_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        enemy = Worlds.FindCardOnField(
            effect,
            name="Prowler",
            card_type=Enemy
        )
        do_attack = False
        defeats_character = False
        if enemy:
            units = Worlds.GetPlayersIdentities(effect)
            face = Filter.One(units, effect, fewest_remaining_hp=True)
            if face:
                def action(face: 'CardFace'):
                    nonlocal defeats_character
                    defeats_character = True
                do_attack = enemy.BasicAttack([face], effect, if_this_defeat_unit=action)
        if defeats_character or not do_attack:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            slice_and_dice_revealed
        ),
    ]

