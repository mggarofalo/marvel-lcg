from . import *

# Break a Leg

def GetAbilities() -> Sequence['Ability']:

    def break_a_leg_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Stunned", effect)
        num = 2
        champion = FindTheChampion(effect)
        challengers = FindTheChallengers(effect)
        if champion and challengers:
            if challengers.GetCounters('ratings') > champion.GetCounters('ratings'):
                num = 4
            place_num = player.DeclareNumber(0, num)
            if place_num:
                num -= place_num
                if champion:
                    Faces.PlaceCountersOn([champion], place_num, 'ratings', effect)

        identity.TakeDamage(this, num, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            break_a_leg_revealed
        ),
    ]

