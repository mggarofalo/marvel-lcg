from . import *

# Specialized Training

def GetAbilities() -> Sequence['Ability']:

    def specialized_training(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.GetControlUpgrade(CardFinder2("SPECIALIZATION", Upgrade))
            if not faces:

                face = Search.SearchForCard(
                    effect,
                    player,
                    include_set_aside=True,
                    trait="SPECIALIZATION",
                    card_type=Upgrade,
                )
                if face:
                    face.PutIntoPlay(player, effect, under_control=True)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            specialized_training,
        ),
    ]

