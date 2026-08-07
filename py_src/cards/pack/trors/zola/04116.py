from . import *

# Berserk Mutate

def GetAbilities() -> Sequence['Ability']:

    def berserk_mutate(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            Faces.PlaceCountersOn([scheme], 1, 'test', effect)
            test_count = scheme.GetCounters('test')
        else:
            test_count = 0

        zola = Worlds.FindVillain(effect)

        if zola:
            zola.GainForThisActive(effect, message.being_message, attack=test_count, scheme=test_count)

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            berserk_mutate
        )
    ]

