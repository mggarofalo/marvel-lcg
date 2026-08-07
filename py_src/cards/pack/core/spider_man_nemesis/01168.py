from . import *

# Sweeping Swoop

def GetAbilities() -> Sequence['Ability']:

    def sweeping_swoop(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if player.IsHero():
            Faces.GiveStatus([player.GetHero()], "Stunned", effect)

        vulture = Worlds.FindCardOnField(
            effect,
            name="Vulture",
            card_type=Minion
        )
        if vulture:
            this.GainSurge(1, effect)

    def sweeping_swoop_boost(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        Faces.GiveStatus([message.trigger], "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sweeping_swoop
        ),
        AbilityFactory.BoostIfThisActivationDealsDamage(
            Friend,
            sweeping_swoop_boost
        ),
    ]
