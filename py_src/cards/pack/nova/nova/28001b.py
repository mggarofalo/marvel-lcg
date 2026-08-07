from . import *

# * Sam Alexander

def GetAbilities() -> Sequence['Ability']:

    def sam_alexander(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Supernova Helmet",
            card_type=Upgrade,
        )
        if face:
            if effect.GetPaidResources().HasColor("G"):
                face.PutIntoPlay(initiator, effect)
            else:
                initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            sam_alexander
        ).SetCost(Cost("1")),
    ]

