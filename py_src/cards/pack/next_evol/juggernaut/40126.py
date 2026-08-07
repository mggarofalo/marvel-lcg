from . import *

# Flatten

def GetAbilities() -> Sequence['Ability']:

    def flatten_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        juggernaut = Worlds.FindCardOnField(effect, name="Juggernaut", card_type=HasAttack)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take damage equal to Juggernaut's ATK",
                lambda targets:
                    identity.TakeDamage(this, juggernaut.attack, effect),
            ).SetTarget("YourIdentity") if juggernaut else None,
            AbilityFactory.ForChoiceAbility(
                "Place 1 momentum counter on him",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'momentum', effect)
            ).SetTarget(name="Juggernaut"),
        )

    def flatten_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Juggernaut",
            card_type=Villain
        )
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            flatten_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            flatten_boost
        ),
    ]

