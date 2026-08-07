from . import *

# Evil Alliance

def GetAbilities() -> Sequence['Ability']:

    def evil_alliance_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            env = GetPursuedByThePast(effect)
            if env:
                Faces.PlaceCountersOn([env], 3, 'pursuit', effect)
        Worlds.Enemies.AllMinionActivateAgainstYou(effect, player, finder=CardFinder(is_nemesis=True), if_no_activate=action)


    def evil_alliance_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            env = GetPursuedByThePast(effect)
            if env:
                Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
        message.AfterThisActivation(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            evil_alliance_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            evil_alliance_boost
        ),
    ]

