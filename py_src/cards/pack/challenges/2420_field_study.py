from . import *

# Field Study

def GetAbilities() -> Sequence['Ability']:

    def field_study(effect: 'Effect', message: 'Message.WhenCardRevealed'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            trait="SETTING",
            card_type=Environment,
        )

        player = message.GetToPlayer()

        if face:
            face.ResolveSpecialAbility(player, effect)

    return [
        AbilityFactory.UsingOnlyHeroes(
            alter_ego_has_traits=["GENIUS"]
        ),
        AbilityFactory.WhenCardRevealed(
            AbilityType.Challenge,
            Minion,
            field_study
        ),
    ]

