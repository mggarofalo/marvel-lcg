from . import *

# Single-Minded Fury

def GetAbilities() -> Sequence['Ability']:

    def single_minded_fury_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            if IsControlPowerStone(player.GetIdentity()):
                villain = Worlds.FindVillain(effect)
                if villain:
                    if villain.DoAttackYou(player, effect):
                        return True
            return False

        do_attack = Players.ForEachPlayer(effect, action, False)

        if not do_attack:
            ThisCardGainSurge(effect)

    def single_minded_fury_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        face = GetPowerStone(effect)
        if face:
            villain = Worlds.FindVillain(effect)
            if villain:
                face.AttachTo2(villain, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            single_minded_fury_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            single_minded_fury_boost
        ),
    ]

