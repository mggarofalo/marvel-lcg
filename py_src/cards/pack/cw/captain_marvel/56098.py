from . import *

# Energy Channel

def GetAbilities() -> Sequence['Ability']:

    def energy_channel(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'energy', effect)

        if this.GetCounters('energy') >= 4:
            Faces.RemoveCountersOn([this], "All", 'energy', effect)

            villain = FindCaptainMarvel(effect)
            if villain:
                villain.BasicAttack([message.attacker], effect, property=AttackProperty(overkill=True))


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            CardFinder(card_type=Leader|Hero|Ally),
            CardFinder(name="Captain Marvel"),
            energy_channel
        ),
    ]

