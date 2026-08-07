from . import *

# Magnetic Missile

def GetAbilities() -> Sequence['Ability']:

    def magnetic_missile_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.DefeatUnits(targets, this, effect)
            ).SetTarget(Minion, trait="SENTINEL")
        ):

            faces = player.DiscardHandCards((0, 5), effect)
            damage = 5 - len(faces)

            this.DealDamage([identity], damage, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            magnetic_missile_revealed
        ),
    ]

