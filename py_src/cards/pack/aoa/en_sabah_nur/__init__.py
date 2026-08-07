from cards.pack import *

def BiomorphicBlastTechnologicalInterfaceGiantSizedDespot(trait: 'CardFace.TRAITS') -> List['Ability']:

    def biomorphic_blast_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Apocalypse",
            card_type=Villain
        )
        if villain:
            if villain.HasTrait(trait):
                player = message.GetToPlayer()
                villain.DoActivate(player, effect)
            else:
                villain.ChangeToForm(trait, effect)
                scheme = Worlds.FindMainScheme(effect)
                if scheme:
                    Faces.PlaceCountersOn([scheme], 1, 'power', effect)

    def biomorphic_blast_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action():
            villain = Worlds.FindCardOnField(
                effect,
                name="Apocalypse",
                card_type=Villain
            )
            if villain:
                villain.ChangeToForm(trait, effect)

        message.AfterThisActivation(
            effect,
            action
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            biomorphic_blast_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            biomorphic_blast_boost
        ),
    ]

def SourceofPowerPluggedInGiantGrowth(trait: 'CardFace.TRAITS'):

    def source_of_power(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Apocalypse",
            card_type=Villain
        )
        if villain:
            if villain.HasTrait(trait):
                player = message.defeating_player
                if player:
                    villain.DoActivate(player, effect)
            else:
                villain.ChangeToForm(trait, effect)
                Faces.GiveStatus([villain], "Tough", effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            source_of_power,
        ),
    ]

def ApocalypseI(damage: int) -> List['Ability']:

    def apocalypse(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        for identity in units:
            this.DealIndirectDamage(identity, damage, effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            overkill=True,
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "This",
            apocalypse,
            to_this_form=True
        ),
    ]

def ApocalypseII(value: int) -> List['Ability']:

    def apocalypse(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", value, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "This",
            apocalypse,
            to_this_form=True
        ),
    ]


def ApocalypseIII(value: int) -> List['Ability']:

    def apocalypse(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.HealthUnits([this], value, effect)

    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.ForcedResponse,
            "This",
            apocalypse,
            to_this_form=True
        ),
    ]

