from . import *

# * Morlun

def GetAbilities() -> Sequence['Ability']:

    def morlun_gain(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        value = 0
        for identity in units:
            faces = identity.GetPlacedCardArea().FindCards(CardFinder(card_type=EncounterCard))
            value += len(faces)
            ui.extend(faces)

        return value

    def morlun(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        for identity in units:
            faces = identity.GetPlacedCardArea().FindCards(name="Hunting the Spider-Bride")
            Faces.DiscardAll(faces, effect)

    return [
        AbilityFactory.ThisGainKeyword(
            morlun_gain,
            attack=1,
            scheme=1,
            change_on_event=OnEvent.TuckUnder("Identity")
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            morlun
        ),
    ]

