from . import *

# Adaptoid

def GetAbilities() -> Sequence['Ability']:

    def adaptoid(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(effect, CardFinder(card_type=Environment, has_counter='all-purpose'))
        face = Filter.One(faces, effect)
        if face:
            Faces.RemoveCountersOn([face], 1, 'all-purpose', effect)

    def adaptoid_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action():
            Faces.ShuffleAllTo([this], "EncounterDeck", effect)

        message.AfterThisActivation(effect, action)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            adaptoid
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            adaptoid_boost
        ),
    ]

