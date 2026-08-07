from . import *

# Beetle Mania

def GetAbilities() -> Sequence['Ability']:

    def beetle_mania_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        do_attack = False

        beetle = Worlds.FindCardOnField(
            effect,
            name="Beetle",
            card_type=Minion
        )
        if beetle:
            if beetle.DoAttackYou(player, effect, property=AttackProperty(additional_value=1)):
                do_attack = True

        if not do_attack:
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            beetle_mania_hero
        ),
    ]

