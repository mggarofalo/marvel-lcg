from . import *

# A.I.M. Abductor

def GetAbilities() -> Sequence['Ability']:

    def aim_abductor_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        scheme = Worlds.FindCardOnField(effect, name="Abduct Superhumans")

        ally = Filter.One(player.GetControlAllies(), effect, most_remaining_hp=True)
        if ally and scheme:
            scheme.TuckCardUnderHere(ally, effect)
        else:
            if scheme:
                this.PlaceThreatOnSchemes([scheme], 2, effect)
        if not scheme:
            Find.FindAndPutIntoPlay(
                effect,
                player,
                name="Abduct Superhumans"
            )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            aim_abductor_revealed
        ),
    ]

