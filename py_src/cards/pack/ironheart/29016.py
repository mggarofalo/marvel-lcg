from . import *

# * Patriot: Rayshaun Lucas

def GetAbilities() -> Sequence['Ability']:

    def patriot(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        for face in effect.targets:
            face.GainUntilRoundEnd(
                effect,
                attack=+1,
                defense=+1,
                thwart=+1,
            )

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            patriot
        ).SetTarget(Unit2, trait="CHAMPION"),
    ]

