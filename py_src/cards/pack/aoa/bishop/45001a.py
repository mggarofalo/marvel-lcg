from . import *

# * Bishop

def GetAbilities() -> Sequence['Ability']:

    def bishop(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        size = message.took_damage
        faces = initiator.DiscardDeckTopCards(size, effect)
        faces = Filter.ByType(faces, Resource)
        initiator.GainCard(faces, effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.Response,
            "This",
            bishop,
            is_from_attack=True
        ).SetName("Energy Absorption"),
    ]

