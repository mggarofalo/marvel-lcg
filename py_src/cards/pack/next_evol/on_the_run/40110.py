from . import *

# Dizzying Deeds

def GetAbilities() -> Sequence['Ability']:

    def dizzying_deeds_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True)
        )

        arclight = Worlds.FindCardOnField(
            effect,
            name="Arclight",
            card_type=Enemy
        )
        if arclight:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Stunned", effect)
                ).SetTarget("YouControlUnit", canbe_stunned=True)
            )

        riptide = Worlds.FindCardOnField(
            effect,
            name="Riptide",
            card_type=Enemy)
        if riptide:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        identity.TakeIndirectDamage(this, 3, effect)
                )
            )

        vertigo = Worlds.FindCardOnField(
            effect,
            name="Vertigo",
            card_type=Enemy
        )
        if vertigo:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.GiveStatus(targets, "Confused", effect)
                ).SetTarget("YouControlUnit", canbe_confused=True)
            )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dizzying_deeds_revealed
        ),
    ]

