from . import *

# Concussive Blast

def GetAbilities() -> Sequence['Ability']:

    def concussive_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 6, effect)
        face = Worlds.FindCardOnField(
            effect,
            name="Bishop",
            card_type=Hero
        )
        if face and effect.IsPaidForWithCardType(Resource):
            Faces.ReadyAll([face], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            concussive_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

