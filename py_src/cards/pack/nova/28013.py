from . import *

# No Quarter

def GetAbilities() -> Sequence['Ability']:

    def no_quarter(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(num: int):
            faces = initiator.DiscardDeckTopCards(num, effect)
            aggression: List['ClassCard'] = []
            for face in faces:
                if ClassCard.IsType(face) and face.IsClass("Aggression"):
                    aggression.append(face)
            initiator.GainCard(aggression, effect)

        this.DealDamage(effect.targets, 4, effect, if_this_attack_deal_excess_damage=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            no_quarter
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

