from . import *

# * Valkyrie

def GetAbilities() -> Sequence['Ability']:

    def valkyrie(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        face = FindDeathGlow(effect)
        if face:
            initiator.PlayCardsLikeInTurn([face], effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            valkyrie,
            conditions=[
                lambda effect, message:
                    DeathGlowIsSetAside(effect),
            ]
        ).SetName("Death Perception"),
    ]

