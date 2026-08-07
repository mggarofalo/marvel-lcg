from . import *

# Escape Plan

def GetAbilities() -> Sequence['Ability']:

    def escape_plan(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        identity = message.GetToPlayer().GetIdentity()
        player = message.GetToPlayer()

        if identity.IsConfused():
            villain = GetPiledriver(effect)
            villain.DoSchemes(player, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)

    def escape_plan_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            if villain.IsTough():
                scheme = villain.FindCorrespondingScheme()
                if scheme:
                    this.PlaceThreatOnSchemes([scheme], 2, effect)
            else:
                Faces.GiveStatus([villain], "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            escape_plan
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            escape_plan_boost
        )
    ]

