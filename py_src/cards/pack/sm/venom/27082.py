from . import *

# Biting Retort

def GetAbilities() -> Sequence['Ability']:

    def biting_retort_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        boost_effects = this.effect.Registers(
            AbilityFactory.WhenBoostIconsWouldBeCount(
                AbilityType.Temp0,
                lambda boost_effect, message:
                    message.UpdateBoostIcon(+1, effect),
            )
        )

        villain = Worlds.FindCardOnField(
            effect,
            name="Venom",
            card_type=Villain
        )
        if villain:
            villain.DoActivate(player, effect)

        Effects.UnRegister(boost_effects)


    def biting_retort_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        bell_tower = FindBellTower(effect)
        if bell_tower:
            Faces.RemoveCountersOn([bell_tower], 1, 'chime', effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            biting_retort_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            biting_retort_boost
        ),
    ]

