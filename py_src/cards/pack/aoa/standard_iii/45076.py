from . import *

# Dark Design

def GetAbilities() -> Sequence['Ability']:

    def dark_design_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        env = GetPursuedByThePast(effect)
        if env:
            Faces.PlaceCountersOn([env], 1, 'pursuit', effect)
            if env.GetAllCounters() > 0:
                villain = Worlds.FindVillain(effect)
                if villain:
                    villain.DoSchemes(player, effect)

    def dark_design_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
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
            dark_design_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            dark_design_boost
        ),
    ]

