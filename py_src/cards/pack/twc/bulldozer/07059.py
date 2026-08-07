from . import *

# Leading the Charge

def GetAbilities() -> Sequence['Ability']:

    def leading_the_charge(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = GetBulldozer(effect)

        faces = player.DiscardDeckTopCards(villain.attack, effect)

        count = FacesCounter.GetDifferentTypesCount(faces)

        scheme = villain.FindCorrespondingScheme()
        if scheme:
            this.PlaceThreatOnSchemes([scheme], count, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            leading_the_charge
        )
    ]

