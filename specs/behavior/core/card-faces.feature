@core
Feature: Canonical Core printed card faces
  Each scenario places the named physical face in a legal Core deal and
  checks the structured facts generated from that same printed authority.

  @behavior:card:01001a:printed-name
  @covers:behavior:card:01001a:printed-type
  @covers:behavior:card:01001a:printed-traits
  @covers:behavior:card:01001a:printed-atk
  @covers:behavior:card:01001a:printed-def
  @covers:behavior:card:01001a:printed-hp
  @covers:behavior:card:01001a:printed-hs
  @covers:behavior:card:01001a:printed-thw
  @covers:behavior:card:01001a:printed-unique
  @card:01001a
  Scenario: Card 01001a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 500 |
    When the printed characteristics of card 01001a copy 0 are requested
    Then card 01001a copy 0 exposes these printed characteristics
      | field | value |
      | name | Spider-Man |
      | type | Hero |
      | traits | AVENGER |
      | attribute:ATK | 2 |
      | attribute:DEF | 3 |
      | attribute:HP | 10 |
      | attribute:HS | 5 |
      | attribute:THW | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01001b:printed-name
  @covers:behavior:card:01001b:printed-type
  @covers:behavior:card:01001b:printed-traits
  @covers:behavior:card:01001b:printed-hp
  @covers:behavior:card:01001b:printed-hs
  @covers:behavior:card:01001b:printed-rec
  @covers:behavior:card:01001b:printed-unique
  @card:01001b
  Scenario: Card 01001b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 501 |
    When the printed characteristics of card 01001b copy 0 are requested
    Then card 01001b copy 0 exposes these printed characteristics
      | field | value |
      | name | Peter Parker |
      | type | AlterEgo |
      | traits | GENIUS |
      | attribute:HP | 10 |
      | attribute:HS | 6 |
      | attribute:REC | 3 |
      | attribute:Unique | 1 |

  @behavior:card:01002:printed-name
  @covers:behavior:card:01002:printed-subtitle
  @covers:behavior:card:01002:printed-type
  @covers:behavior:card:01002:printed-traits
  @covers:behavior:card:01002:printed-atk
  @covers:behavior:card:01002:printed-class
  @covers:behavior:card:01002:printed-cost
  @covers:behavior:card:01002:printed-hp
  @covers:behavior:card:01002:printed-res
  @covers:behavior:card:01002:printed-thw
  @covers:behavior:card:01002:printed-unique
  @card:01002
  Scenario: Card 01002 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 502 |
    When the printed characteristics of card 01002 copy 0 are requested
    Then card 01002 copy 0 exposes these printed characteristics
      | field | value |
      | name | Black Cat |
      | subtitle | Felicia Hardy |
      | type | Ally |
      | traits | HERO FOR HIRE |
      | attribute:ATK | 1 |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:HP | 2 |
      | attribute:RES | Y |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01003:printed-name
  @covers:behavior:card:01003:printed-type
  @covers:behavior:card:01003:printed-traits
  @covers:behavior:card:01003:printed-class
  @covers:behavior:card:01003:printed-cost
  @covers:behavior:card:01003:printed-res
  @card:01003
  Scenario: Card 01003 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 503 |
    When the printed characteristics of card 01003 copy 0 are requested
    Then card 01003 copy 0 exposes these printed characteristics
      | field | value |
      | name | Backflip |
      | type | Event |
      | traits | DEFENSE/SKILL |
      | attribute:Class | Hero |
      | attribute:Cost | 0 |
      | attribute:RES | R |

  @behavior:card:01004:printed-name
  @covers:behavior:card:01004:printed-type
  @covers:behavior:card:01004:printed-traits
  @covers:behavior:card:01004:printed-class
  @covers:behavior:card:01004:printed-cost
  @covers:behavior:card:01004:printed-res
  @card:01004
  Scenario: Card 01004 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 504 |
    When the printed characteristics of card 01004 copy 0 are requested
    Then card 01004 copy 0 exposes these printed characteristics
      | field | value |
      | name | Enhanced Spider-Sense |
      | type | Event |
      | traits | SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01005:printed-name
  @covers:behavior:card:01005:printed-type
  @covers:behavior:card:01005:printed-traits
  @covers:behavior:card:01005:printed-class
  @covers:behavior:card:01005:printed-cost
  @covers:behavior:card:01005:printed-res
  @card:01005
  Scenario: Card 01005 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 505 |
    When the printed characteristics of card 01005 copy 0 are requested
    Then card 01005 copy 0 exposes these printed characteristics
      | field | value |
      | name | Swinging Web Kick |
      | type | Event |
      | traits | AERIAL/ATTACK/SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | B |

  @behavior:card:01006:printed-name
  @covers:behavior:card:01006:printed-type
  @covers:behavior:card:01006:printed-traits
  @covers:behavior:card:01006:printed-class
  @covers:behavior:card:01006:printed-cost
  @covers:behavior:card:01006:printed-res
  @covers:behavior:card:01006:printed-unique
  @card:01006
  Scenario: Card 01006 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 506 |
    When the printed characteristics of card 01006 copy 0 are requested
    Then card 01006 copy 0 exposes these printed characteristics
      | field | value |
      | name | Aunt May |
      | type | Support |
      | traits | PERSONA |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | Y |
      | attribute:Unique | 1 |

  @behavior:card:01007:printed-name
  @covers:behavior:card:01007:printed-type
  @covers:behavior:card:01007:printed-traits
  @covers:behavior:card:01007:printed-class
  @covers:behavior:card:01007:printed-cost
  @covers:behavior:card:01007:printed-res
  @card:01007
  Scenario: Card 01007 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 507 |
    When the printed characteristics of card 01007 copy 0 are requested
    Then card 01007 copy 0 exposes these printed characteristics
      | field | value |
      | name | Spider-Tracer |
      | type | Upgrade |
      | traits | ITEM/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | Y |

  @behavior:card:01008:printed-name
  @covers:behavior:card:01008:printed-type
  @covers:behavior:card:01008:printed-traits
  @covers:behavior:card:01008:printed-class
  @covers:behavior:card:01008:printed-cost
  @covers:behavior:card:01008:printed-res
  @covers:behavior:card:01008:printed-uses
  @card:01008
  Scenario: Card 01008 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 508 |
    When the printed characteristics of card 01008 copy 0 are requested
    Then card 01008 copy 0 exposes these printed characteristics
      | field | value |
      | name | Web-Shooter |
      | type | Upgrade |
      | traits | ITEM/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |
      | attribute:Uses | 3,web |

  @behavior:card:01009:printed-name
  @covers:behavior:card:01009:printed-type
  @covers:behavior:card:01009:printed-traits
  @covers:behavior:card:01009:printed-class
  @covers:behavior:card:01009:printed-cost
  @covers:behavior:card:01009:printed-maxperunit
  @covers:behavior:card:01009:printed-maxperunitkind
  @covers:behavior:card:01009:printed-res
  @card:01009
  Scenario: Card 01009 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 509 |
    When the printed characteristics of card 01009 copy 0 are requested
    Then card 01009 copy 0 exposes these printed characteristics
      | field | value |
      | name | Webbed Up |
      | type | Upgrade |
      | traits | CONDITION |
      | attribute:Class | Hero |
      | attribute:Cost | 4 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | enemy |
      | attribute:RES | R |

  @behavior:card:01010a:printed-name
  @covers:behavior:card:01010a:printed-type
  @covers:behavior:card:01010a:printed-traits
  @covers:behavior:card:01010a:printed-atk
  @covers:behavior:card:01010a:printed-def
  @covers:behavior:card:01010a:printed-hp
  @covers:behavior:card:01010a:printed-hs
  @covers:behavior:card:01010a:printed-thw
  @covers:behavior:card:01010a:printed-unique
  @card:01010a
  Scenario: Card 01010a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 510 |
    When the printed characteristics of card 01010a copy 0 are requested
    Then card 01010a copy 0 exposes these printed characteristics
      | field | value |
      | name | Captain Marvel |
      | type | Hero |
      | traits | AVENGER/SOLDIER |
      | attribute:ATK | 2 |
      | attribute:DEF | 1 |
      | attribute:HP | 12 |
      | attribute:HS | 5 |
      | attribute:THW | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01010b:printed-name
  @covers:behavior:card:01010b:printed-type
  @covers:behavior:card:01010b:printed-traits
  @covers:behavior:card:01010b:printed-hp
  @covers:behavior:card:01010b:printed-hs
  @covers:behavior:card:01010b:printed-rec
  @covers:behavior:card:01010b:printed-unique
  @card:01010b
  Scenario: Card 01010b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 511 |
    When the printed characteristics of card 01010b copy 0 are requested
    Then card 01010b copy 0 exposes these printed characteristics
      | field | value |
      | name | Carol Danvers |
      | type | AlterEgo |
      | traits | S.H.I.E.L.D/SOLDIER |
      | attribute:HP | 12 |
      | attribute:HS | 6 |
      | attribute:REC | 4 |
      | attribute:Unique | 1 |

  @behavior:card:01011:printed-name
  @covers:behavior:card:01011:printed-subtitle
  @covers:behavior:card:01011:printed-type
  @covers:behavior:card:01011:printed-traits
  @covers:behavior:card:01011:printed-atk
  @covers:behavior:card:01011:printed-class
  @covers:behavior:card:01011:printed-cost
  @covers:behavior:card:01011:printed-hp
  @covers:behavior:card:01011:printed-res
  @covers:behavior:card:01011:printed-thw
  @covers:behavior:card:01011:printed-unique
  @card:01011
  Scenario: Card 01011 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 512 |
    When the printed characteristics of card 01011 copy 0 are requested
    Then card 01011 copy 0 exposes these printed characteristics
      | field | value |
      | name | Spider-Woman |
      | subtitle | Jessica Drew |
      | type | Ally |
      | traits | AVENGER/SPY |
      | attribute:ATK | 2* |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:HP | 2 |
      | attribute:RES | G |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01012:printed-name
  @covers:behavior:card:01012:printed-type
  @covers:behavior:card:01012:printed-traits
  @covers:behavior:card:01012:printed-class
  @covers:behavior:card:01012:printed-cost
  @covers:behavior:card:01012:printed-res
  @card:01012
  Scenario: Card 01012 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 513 |
    When the printed characteristics of card 01012 copy 0 are requested
    Then card 01012 copy 0 exposes these printed characteristics
      | field | value |
      | name | Crisis Interdiction |
      | type | Event |
      | traits | THWART |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01013:printed-name
  @covers:behavior:card:01013:printed-type
  @covers:behavior:card:01013:printed-traits
  @covers:behavior:card:01013:printed-class
  @covers:behavior:card:01013:printed-cost
  @covers:behavior:card:01013:printed-res
  @card:01013
  Scenario: Card 01013 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 514 |
    When the printed characteristics of card 01013 copy 0 are requested
    Then card 01013 copy 0 exposes these printed characteristics
      | field | value |
      | name | Photonic Blast |
      | type | Event |
      | traits | ATTACK/SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | R |

  @behavior:card:01014:printed-name
  @covers:behavior:card:01014:printed-type
  @covers:behavior:card:01014:printed-class
  @covers:behavior:card:01014:printed-res
  @card:01014
  Scenario: Card 01014 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 515 |
    When the printed characteristics of card 01014 copy 0 are requested
    Then card 01014 copy 0 exposes these printed characteristics
      | field | value |
      | name | Energy Absorption |
      | type | Resource |
      | attribute:Class | Hero |
      | attribute:RES | YYY |

  @behavior:card:01015:printed-name
  @covers:behavior:card:01015:printed-type
  @covers:behavior:card:01015:printed-traits
  @covers:behavior:card:01015:printed-class
  @covers:behavior:card:01015:printed-cost
  @covers:behavior:card:01015:printed-res
  @covers:behavior:card:01015:printed-unique
  @card:01015
  Scenario: Card 01015 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 516 |
    When the printed characteristics of card 01015 copy 0 are requested
    Then card 01015 copy 0 exposes these printed characteristics
      | field | value |
      | name | Alpha Flight Station |
      | type | Support |
      | traits | LOCATION/S.H.I.E.L.D |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | B |
      | attribute:Unique | 1 |

  @behavior:card:01016:printed-name
  @covers:behavior:card:01016:printed-type
  @covers:behavior:card:01016:printed-traits
  @covers:behavior:card:01016:printed-class
  @covers:behavior:card:01016:printed-cost
  @covers:behavior:card:01016:printed-res
  @covers:behavior:card:01016:printed-unique
  @card:01016
  Scenario: Card 01016 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 517 |
    When the printed characteristics of card 01016 copy 0 are requested
    Then card 01016 copy 0 exposes these printed characteristics
      | field | value |
      | name | Captain Marvel's Helmet |
      | type | Upgrade |
      | traits | ARMOR/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | R |
      | attribute:Unique | 1 |

  @behavior:card:01017:printed-name
  @covers:behavior:card:01017:printed-type
  @covers:behavior:card:01017:printed-traits
  @covers:behavior:card:01017:printed-class
  @covers:behavior:card:01017:printed-cost
  @covers:behavior:card:01017:printed-res
  @card:01017
  Scenario: Card 01017 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 518 |
    When the printed characteristics of card 01017 copy 0 are requested
    Then card 01017 copy 0 exposes these printed characteristics
      | field | value |
      | name | Cosmic Flight |
      | type | Upgrade |
      | traits | SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01018:printed-name
  @covers:behavior:card:01018:printed-type
  @covers:behavior:card:01018:printed-traits
  @covers:behavior:card:01018:printed-class
  @covers:behavior:card:01018:printed-cost
  @covers:behavior:card:01018:printed-maxperunit
  @covers:behavior:card:01018:printed-maxperunitkind
  @covers:behavior:card:01018:printed-res
  @card:01018
  Scenario: Card 01018 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 519 |
    When the printed characteristics of card 01018 copy 0 are requested
    Then card 01018 copy 0 exposes these printed characteristics
      | field | value |
      | name | Energy Channel |
      | type | Upgrade |
      | traits | SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 0 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | B |

  @behavior:card:01019a:printed-name
  @covers:behavior:card:01019a:printed-type
  @covers:behavior:card:01019a:printed-traits
  @covers:behavior:card:01019a:printed-atk
  @covers:behavior:card:01019a:printed-def
  @covers:behavior:card:01019a:printed-hp
  @covers:behavior:card:01019a:printed-hs
  @covers:behavior:card:01019a:printed-thw
  @covers:behavior:card:01019a:printed-unique
  @card:01019a
  Scenario: Card 01019a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 520 |
    When the printed characteristics of card 01019a copy 0 are requested
    Then card 01019a copy 0 exposes these printed characteristics
      | field | value |
      | name | She-Hulk |
      | type | Hero |
      | traits | AVENGER/GAMMA |
      | attribute:ATK | 3 |
      | attribute:DEF | 2 |
      | attribute:HP | 15 |
      | attribute:HS | 4 |
      | attribute:THW | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01019b:printed-name
  @covers:behavior:card:01019b:printed-type
  @covers:behavior:card:01019b:printed-traits
  @covers:behavior:card:01019b:printed-hp
  @covers:behavior:card:01019b:printed-hs
  @covers:behavior:card:01019b:printed-rec
  @covers:behavior:card:01019b:printed-unique
  @card:01019b
  Scenario: Card 01019b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 521 |
    When the printed characteristics of card 01019b copy 0 are requested
    Then card 01019b copy 0 exposes these printed characteristics
      | field | value |
      | name | Jennifer Walters |
      | type | AlterEgo |
      | traits | ATTORNEY/GAMMA |
      | attribute:HP | 15 |
      | attribute:HS | 6 |
      | attribute:REC | 5 |
      | attribute:Unique | 1 |

  @behavior:card:01020:printed-name
  @covers:behavior:card:01020:printed-subtitle
  @covers:behavior:card:01020:printed-type
  @covers:behavior:card:01020:printed-traits
  @covers:behavior:card:01020:printed-atk
  @covers:behavior:card:01020:printed-class
  @covers:behavior:card:01020:printed-cost
  @covers:behavior:card:01020:printed-hp
  @covers:behavior:card:01020:printed-res
  @covers:behavior:card:01020:printed-thw
  @covers:behavior:card:01020:printed-unique
  @card:01020
  Scenario: Card 01020 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 522 |
    When the printed characteristics of card 01020 copy 0 are requested
    Then card 01020 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hellcat |
      | subtitle | Patsy Walker |
      | type | Ally |
      | traits | AVENGER |
      | attribute:ATK | 1* |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:HP | 3 |
      | attribute:RES | G |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01021:printed-name
  @covers:behavior:card:01021:printed-type
  @covers:behavior:card:01021:printed-traits
  @covers:behavior:card:01021:printed-class
  @covers:behavior:card:01021:printed-cost
  @covers:behavior:card:01021:printed-res
  @card:01021
  Scenario: Card 01021 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 523 |
    When the printed characteristics of card 01021 copy 0 are requested
    Then card 01021 copy 0 exposes these printed characteristics
      | field | value |
      | name | Gamma Slam |
      | type | Event |
      | traits | ATTACK/SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 4 |
      | attribute:RES | B |

  @behavior:card:01022:printed-name
  @covers:behavior:card:01022:printed-type
  @covers:behavior:card:01022:printed-traits
  @covers:behavior:card:01022:printed-class
  @covers:behavior:card:01022:printed-cost
  @covers:behavior:card:01022:printed-res
  @card:01022
  Scenario: Card 01022 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 524 |
    When the printed characteristics of card 01022 copy 0 are requested
    Then card 01022 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ground Stomp |
      | type | Event |
      | traits | SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | B |

  @behavior:card:01023:printed-name
  @covers:behavior:card:01023:printed-type
  @covers:behavior:card:01023:printed-traits
  @covers:behavior:card:01023:printed-class
  @covers:behavior:card:01023:printed-cost
  @covers:behavior:card:01023:printed-res
  @card:01023
  Scenario: Card 01023 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 525 |
    When the printed characteristics of card 01023 copy 0 are requested
    Then card 01023 copy 0 exposes these printed characteristics
      | field | value |
      | name | Legal Practice |
      | type | Event |
      | traits | SKILL/THWART |
      | attribute:Class | Hero |
      | attribute:Cost | 0 |
      | attribute:RES | R |

  @behavior:card:01024:printed-name
  @covers:behavior:card:01024:printed-type
  @covers:behavior:card:01024:printed-traits
  @covers:behavior:card:01024:printed-class
  @covers:behavior:card:01024:printed-cost
  @covers:behavior:card:01024:printed-res
  @card:01024
  Scenario: Card 01024 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 526 |
    When the printed characteristics of card 01024 copy 0 are requested
    Then card 01024 copy 0 exposes these printed characteristics
      | field | value |
      | name | One-Two Punch |
      | type | Event |
      | traits | SKILL |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |

  @behavior:card:01025:printed-name
  @covers:behavior:card:01025:printed-type
  @covers:behavior:card:01025:printed-class
  @covers:behavior:card:01025:printed-cost
  @covers:behavior:card:01025:printed-res
  @card:01025
  Scenario: Card 01025 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 527 |
    When the printed characteristics of card 01025 copy 0 are requested
    Then card 01025 copy 0 exposes these printed characteristics
      | field | value |
      | name | Split Personality |
      | type | Event |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | Y |

  @behavior:card:01026:printed-name
  @covers:behavior:card:01026:printed-type
  @covers:behavior:card:01026:printed-traits
  @covers:behavior:card:01026:printed-class
  @covers:behavior:card:01026:printed-cost
  @covers:behavior:card:01026:printed-res
  @card:01026
  Scenario: Card 01026 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 528 |
    When the printed characteristics of card 01026 copy 0 are requested
    Then card 01026 copy 0 exposes these printed characteristics
      | field | value |
      | name | Superhuman Law Division |
      | type | Support |
      | traits | LOCATION |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |

  @behavior:card:01027:printed-name
  @covers:behavior:card:01027:printed-type
  @covers:behavior:card:01027:printed-traits
  @covers:behavior:card:01027:printed-class
  @covers:behavior:card:01027:printed-cost
  @covers:behavior:card:01027:printed-res
  @card:01027
  Scenario: Card 01027 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 529 |
    When the printed characteristics of card 01027 copy 0 are requested
    Then card 01027 copy 0 exposes these printed characteristics
      | field | value |
      | name | Focused Rage |
      | type | Upgrade |
      | traits | SKILL |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | Y |

  @behavior:card:01028:printed-name
  @covers:behavior:card:01028:printed-type
  @covers:behavior:card:01028:printed-traits
  @covers:behavior:card:01028:printed-class
  @covers:behavior:card:01028:printed-cost
  @covers:behavior:card:01028:printed-res
  @card:01028
  Scenario: Card 01028 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 530 |
    When the printed characteristics of card 01028 copy 0 are requested
    Then card 01028 copy 0 exposes these printed characteristics
      | field | value |
      | name | Superhuman Strength |
      | type | Upgrade |
      | traits | SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | B |

  @behavior:card:01029a:printed-name
  @covers:behavior:card:01029a:printed-type
  @covers:behavior:card:01029a:printed-traits
  @covers:behavior:card:01029a:printed-atk
  @covers:behavior:card:01029a:printed-def
  @covers:behavior:card:01029a:printed-hp
  @covers:behavior:card:01029a:printed-hs
  @covers:behavior:card:01029a:printed-thw
  @covers:behavior:card:01029a:printed-unique
  @card:01029a
  Scenario: Card 01029a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 531 |
    When the printed characteristics of card 01029a copy 0 are requested
    Then card 01029a copy 0 exposes these printed characteristics
      | field | value |
      | name | Iron Man |
      | type | Hero |
      | traits | AVENGER |
      | attribute:ATK | 1 |
      | attribute:DEF | 1 |
      | attribute:HP | 9 |
      | attribute:HS | 1 |
      | attribute:THW | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01029b:printed-name
  @covers:behavior:card:01029b:printed-type
  @covers:behavior:card:01029b:printed-traits
  @covers:behavior:card:01029b:printed-hp
  @covers:behavior:card:01029b:printed-hs
  @covers:behavior:card:01029b:printed-rec
  @covers:behavior:card:01029b:printed-unique
  @card:01029b
  Scenario: Card 01029b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 532 |
    When the printed characteristics of card 01029b copy 0 are requested
    Then card 01029b copy 0 exposes these printed characteristics
      | field | value |
      | name | Tony Stark |
      | type | AlterEgo |
      | traits | GENIUS |
      | attribute:HP | 9 |
      | attribute:HS | 6 |
      | attribute:REC | 3 |
      | attribute:Unique | 1 |

  @behavior:card:01030:printed-name
  @covers:behavior:card:01030:printed-subtitle
  @covers:behavior:card:01030:printed-type
  @covers:behavior:card:01030:printed-traits
  @covers:behavior:card:01030:printed-atk
  @covers:behavior:card:01030:printed-class
  @covers:behavior:card:01030:printed-cost
  @covers:behavior:card:01030:printed-hp
  @covers:behavior:card:01030:printed-res
  @covers:behavior:card:01030:printed-thw
  @covers:behavior:card:01030:printed-unique
  @card:01030
  Scenario: Card 01030 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 533 |
    When the printed characteristics of card 01030 copy 0 are requested
    Then card 01030 copy 0 exposes these printed characteristics
      | field | value |
      | name | War Machine |
      | subtitle | James Rhodes |
      | type | Ally |
      | traits | S.H.I.E.L.D/SOLDIER |
      | attribute:ATK | 2* |
      | attribute:Class | Hero |
      | attribute:Cost | 4 |
      | attribute:HP | 4 |
      | attribute:RES | G |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01031:printed-name
  @covers:behavior:card:01031:printed-type
  @covers:behavior:card:01031:printed-traits
  @covers:behavior:card:01031:printed-class
  @covers:behavior:card:01031:printed-cost
  @covers:behavior:card:01031:printed-res
  @card:01031
  Scenario: Card 01031 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 534 |
    When the printed characteristics of card 01031 copy 0 are requested
    Then card 01031 copy 0 exposes these printed characteristics
      | field | value |
      | name | Repulsor Blast |
      | type | Event |
      | traits | ATTACK/SUPERPOWER |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |

  @behavior:card:01032:printed-name
  @covers:behavior:card:01032:printed-type
  @covers:behavior:card:01032:printed-traits
  @covers:behavior:card:01032:printed-class
  @covers:behavior:card:01032:printed-cost
  @covers:behavior:card:01032:printed-res
  @card:01032
  Scenario: Card 01032 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 535 |
    When the printed characteristics of card 01032 copy 0 are requested
    Then card 01032 copy 0 exposes these printed characteristics
      | field | value |
      | name | Supersonic Punch |
      | type | Event |
      | traits | ATTACK |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01033:printed-name
  @covers:behavior:card:01033:printed-type
  @covers:behavior:card:01033:printed-traits
  @covers:behavior:card:01033:printed-class
  @covers:behavior:card:01033:printed-cost
  @covers:behavior:card:01033:printed-res
  @covers:behavior:card:01033:printed-unique
  @card:01033
  Scenario: Card 01033 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 536 |
    When the printed characteristics of card 01033 copy 0 are requested
    Then card 01033 copy 0 exposes these printed characteristics
      | field | value |
      | name | Pepper Potts |
      | type | Support |
      | traits | PERSONA |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | R |
      | attribute:Unique | 1 |

  @behavior:card:01034:printed-name
  @covers:behavior:card:01034:printed-type
  @covers:behavior:card:01034:printed-traits
  @covers:behavior:card:01034:printed-class
  @covers:behavior:card:01034:printed-cost
  @covers:behavior:card:01034:printed-res
  @covers:behavior:card:01034:printed-unique
  @card:01034
  Scenario: Card 01034 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 537 |
    When the printed characteristics of card 01034 copy 0 are requested
    Then card 01034 copy 0 exposes these printed characteristics
      | field | value |
      | name | Stark Tower |
      | type | Support |
      | traits | LOCATION |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | B |
      | attribute:Unique | 1 |

  @behavior:card:01035:printed-name
  @covers:behavior:card:01035:printed-type
  @covers:behavior:card:01035:printed-traits
  @covers:behavior:card:01035:printed-class
  @covers:behavior:card:01035:printed-cost
  @covers:behavior:card:01035:printed-res
  @covers:behavior:card:01035:printed-unique
  @card:01035
  Scenario: Card 01035 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 538 |
    When the printed characteristics of card 01035 copy 0 are requested
    Then card 01035 copy 0 exposes these printed characteristics
      | field | value |
      | name | Arc Reactor |
      | type | Upgrade |
      | traits | ITEM/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |
      | attribute:Unique | 1 |

  @behavior:card:01036:printed-name
  @covers:behavior:card:01036:printed-type
  @covers:behavior:card:01036:printed-traits
  @covers:behavior:card:01036:printed-class
  @covers:behavior:card:01036:printed-cost
  @covers:behavior:card:01036:printed-res
  @covers:behavior:card:01036:printed-unique
  @card:01036
  Scenario: Card 01036 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 539 |
    When the printed characteristics of card 01036 copy 0 are requested
    Then card 01036 copy 0 exposes these printed characteristics
      | field | value |
      | name | Mark V Armor |
      | type | Upgrade |
      | traits | ARMOR/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 3 |
      | attribute:RES | B |
      | attribute:Unique | 1 |

  @behavior:card:01037:printed-name
  @covers:behavior:card:01037:printed-type
  @covers:behavior:card:01037:printed-traits
  @covers:behavior:card:01037:printed-class
  @covers:behavior:card:01037:printed-cost
  @covers:behavior:card:01037:printed-res
  @covers:behavior:card:01037:printed-unique
  @card:01037
  Scenario: Card 01037 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 540 |
    When the printed characteristics of card 01037 copy 0 are requested
    Then card 01037 copy 0 exposes these printed characteristics
      | field | value |
      | name | Mark V Helmet |
      | type | Upgrade |
      | traits | ARMOR/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |
      | attribute:Unique | 1 |

  @behavior:card:01038:printed-name
  @covers:behavior:card:01038:printed-type
  @covers:behavior:card:01038:printed-traits
  @covers:behavior:card:01038:printed-class
  @covers:behavior:card:01038:printed-cost
  @covers:behavior:card:01038:printed-res
  @card:01038
  Scenario: Card 01038 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 541 |
    When the printed characteristics of card 01038 copy 0 are requested
    Then card 01038 copy 0 exposes these printed characteristics
      | field | value |
      | name | Powered Gauntlets |
      | type | Upgrade |
      | traits | ARMOR/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01039:printed-name
  @covers:behavior:card:01039:printed-type
  @covers:behavior:card:01039:printed-traits
  @covers:behavior:card:01039:printed-class
  @covers:behavior:card:01039:printed-cost
  @covers:behavior:card:01039:printed-res
  @card:01039
  Scenario: Card 01039 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 542 |
    When the printed characteristics of card 01039 copy 0 are requested
    Then card 01039 copy 0 exposes these printed characteristics
      | field | value |
      | name | Rocket Boots |
      | type | Upgrade |
      | traits | ARMOR/TECH |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01040a:printed-name
  @covers:behavior:card:01040a:printed-type
  @covers:behavior:card:01040a:printed-traits
  @covers:behavior:card:01040a:printed-atk
  @covers:behavior:card:01040a:printed-def
  @covers:behavior:card:01040a:printed-hp
  @covers:behavior:card:01040a:printed-hs
  @covers:behavior:card:01040a:printed-retaliate
  @covers:behavior:card:01040a:printed-thw
  @covers:behavior:card:01040a:printed-unique
  @card:01040a
  Scenario: Card 01040a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 543 |
    When the printed characteristics of card 01040a copy 0 are requested
    Then card 01040a copy 0 exposes these printed characteristics
      | field | value |
      | name | Black Panther |
      | type | Hero |
      | traits | AVENGER/WAKANDA |
      | attribute:ATK | 2 |
      | attribute:DEF | 2 |
      | attribute:HP | 11 |
      | attribute:HS | 5 |
      | attribute:Retaliate | 1 |
      | attribute:THW | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01040b:printed-name
  @covers:behavior:card:01040b:printed-type
  @covers:behavior:card:01040b:printed-traits
  @covers:behavior:card:01040b:printed-hp
  @covers:behavior:card:01040b:printed-hs
  @covers:behavior:card:01040b:printed-rec
  @covers:behavior:card:01040b:printed-unique
  @card:01040b
  Scenario: Card 01040b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 544 |
    When the printed characteristics of card 01040b copy 0 are requested
    Then card 01040b copy 0 exposes these printed characteristics
      | field | value |
      | name | T'Challa |
      | type | AlterEgo |
      | traits | KING/WAKANDA |
      | attribute:HP | 11 |
      | attribute:HS | 6 |
      | attribute:REC | 4 |
      | attribute:Unique | 1 |

  @behavior:card:01041:printed-name
  @covers:behavior:card:01041:printed-type
  @covers:behavior:card:01041:printed-traits
  @covers:behavior:card:01041:printed-atk
  @covers:behavior:card:01041:printed-class
  @covers:behavior:card:01041:printed-cost
  @covers:behavior:card:01041:printed-hp
  @covers:behavior:card:01041:printed-res
  @covers:behavior:card:01041:printed-thw
  @covers:behavior:card:01041:printed-unique
  @card:01041
  Scenario: Card 01041 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 545 |
    When the printed characteristics of card 01041 copy 0 are requested
    Then card 01041 copy 0 exposes these printed characteristics
      | field | value |
      | name | Shuri |
      | type | Ally |
      | traits | GENIUS/WAKANDA |
      | attribute:ATK | 1* |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:HP | 3 |
      | attribute:RES | R |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01042:printed-name
  @covers:behavior:card:01042:printed-type
  @covers:behavior:card:01042:printed-class
  @covers:behavior:card:01042:printed-cost
  @covers:behavior:card:01042:printed-res
  @card:01042
  Scenario: Card 01042 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 546 |
    When the printed characteristics of card 01042 copy 0 are requested
    Then card 01042 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ancestral Knowledge |
      | type | Event |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01043a:printed-name
  @covers:behavior:card:01043a:printed-type
  @covers:behavior:card:01043a:printed-traits
  @covers:behavior:card:01043a:printed-class
  @covers:behavior:card:01043a:printed-cost
  @covers:behavior:card:01043a:printed-res
  @card:01043a
  Scenario: Card 01043a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 547 |
    When the printed characteristics of card 01043a copy 0 are requested
    Then card 01043a copy 0 exposes these printed characteristics
      | field | value |
      | name | Wakanda Forever! |
      | type | Event |
      | traits | TACTIC |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | Y |

  @behavior:card:01043b:printed-name
  @covers:behavior:card:01043b:printed-type
  @covers:behavior:card:01043b:printed-traits
  @covers:behavior:card:01043b:printed-class
  @covers:behavior:card:01043b:printed-cost
  @covers:behavior:card:01043b:printed-res
  @card:01043b
  Scenario: Card 01043b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 548 |
    When the printed characteristics of card 01043b copy 0 are requested
    Then card 01043b copy 0 exposes these printed characteristics
      | field | value |
      | name | Wakanda Forever! |
      | type | Event |
      | traits | TACTIC |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01043c:printed-name
  @covers:behavior:card:01043c:printed-type
  @covers:behavior:card:01043c:printed-traits
  @covers:behavior:card:01043c:printed-class
  @covers:behavior:card:01043c:printed-cost
  @covers:behavior:card:01043c:printed-res
  @card:01043c
  Scenario: Card 01043c exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 549 |
    When the printed characteristics of card 01043c copy 0 are requested
    Then card 01043c copy 0 exposes these printed characteristics
      | field | value |
      | name | Wakanda Forever! |
      | type | Event |
      | traits | TACTIC |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | R |

  @behavior:card:01043d:printed-name
  @covers:behavior:card:01043d:printed-type
  @covers:behavior:card:01043d:printed-traits
  @covers:behavior:card:01043d:printed-class
  @covers:behavior:card:01043d:printed-cost
  @covers:behavior:card:01043d:printed-res
  @card:01043d
  Scenario: Card 01043d exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 550 |
    When the printed characteristics of card 01043d copy 0 are requested
    Then card 01043d copy 0 exposes these printed characteristics
      | field | value |
      | name | Wakanda Forever! |
      | type | Event |
      | traits | TACTIC |
      | attribute:Class | Hero |
      | attribute:Cost | 1 |
      | attribute:RES | G |

  @behavior:card:01044:printed-name
  @covers:behavior:card:01044:printed-type
  @covers:behavior:card:01044:printed-class
  @covers:behavior:card:01044:printed-res
  @card:01044
  Scenario: Card 01044 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 551 |
    When the printed characteristics of card 01044 copy 0 are requested
    Then card 01044 copy 0 exposes these printed characteristics
      | field | value |
      | name | Vibranium |
      | type | Resource |
      | attribute:Class | Hero |
      | attribute:RES | GG |

  @behavior:card:01045:printed-name
  @covers:behavior:card:01045:printed-type
  @covers:behavior:card:01045:printed-traits
  @covers:behavior:card:01045:printed-class
  @covers:behavior:card:01045:printed-cost
  @covers:behavior:card:01045:printed-res
  @covers:behavior:card:01045:printed-unique
  @card:01045
  Scenario: Card 01045 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 552 |
    When the printed characteristics of card 01045 copy 0 are requested
    Then card 01045 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Golden City |
      | type | Support |
      | traits | LOCATION/WAKANDA |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |
      | attribute:Unique | 1 |

  @behavior:card:01046:printed-name
  @covers:behavior:card:01046:printed-type
  @covers:behavior:card:01046:printed-traits
  @covers:behavior:card:01046:printed-class
  @covers:behavior:card:01046:printed-cost
  @covers:behavior:card:01046:printed-res
  @card:01046
  Scenario: Card 01046 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 553 |
    When the printed characteristics of card 01046 copy 0 are requested
    Then card 01046 copy 0 exposes these printed characteristics
      | field | value |
      | name | Energy Daggers |
      | type | Upgrade |
      | traits | BLACK PANTHER/WEAPON |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | B |

  @behavior:card:01047:printed-name
  @covers:behavior:card:01047:printed-type
  @covers:behavior:card:01047:printed-traits
  @covers:behavior:card:01047:printed-class
  @covers:behavior:card:01047:printed-cost
  @covers:behavior:card:01047:printed-res
  @card:01047
  Scenario: Card 01047 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 554 |
    When the printed characteristics of card 01047 copy 0 are requested
    Then card 01047 copy 0 exposes these printed characteristics
      | field | value |
      | name | Panther Claws |
      | type | Upgrade |
      | traits | BLACK PANTHER/WEAPON |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01048:printed-name
  @covers:behavior:card:01048:printed-type
  @covers:behavior:card:01048:printed-traits
  @covers:behavior:card:01048:printed-class
  @covers:behavior:card:01048:printed-cost
  @covers:behavior:card:01048:printed-res
  @card:01048
  Scenario: Card 01048 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 555 |
    When the printed characteristics of card 01048 copy 0 are requested
    Then card 01048 copy 0 exposes these printed characteristics
      | field | value |
      | name | Tactical Genius |
      | type | Upgrade |
      | traits | BLACK PANTHER/SKILL |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | R |

  @behavior:card:01049:printed-name
  @covers:behavior:card:01049:printed-type
  @covers:behavior:card:01049:printed-traits
  @covers:behavior:card:01049:printed-class
  @covers:behavior:card:01049:printed-cost
  @covers:behavior:card:01049:printed-res
  @card:01049
  Scenario: Card 01049 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 556 |
    When the printed characteristics of card 01049 copy 0 are requested
    Then card 01049 copy 0 exposes these printed characteristics
      | field | value |
      | name | Vibranium Suit |
      | type | Upgrade |
      | traits | ARMOR/BLACK PANTHER |
      | attribute:Class | Hero |
      | attribute:Cost | 2 |
      | attribute:RES | B |

  @behavior:card:01050:printed-name
  @covers:behavior:card:01050:printed-subtitle
  @covers:behavior:card:01050:printed-type
  @covers:behavior:card:01050:printed-traits
  @covers:behavior:card:01050:printed-atk
  @covers:behavior:card:01050:printed-class
  @covers:behavior:card:01050:printed-cost
  @covers:behavior:card:01050:printed-hp
  @covers:behavior:card:01050:printed-res
  @covers:behavior:card:01050:printed-unique
  @card:01050
  Scenario: Card 01050 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 557 |
    When the printed characteristics of card 01050 copy 0 are requested
    Then card 01050 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hulk |
      | subtitle | Bruce Banner |
      | type | Ally |
      | traits | AVENGER/GAMMA |
      | attribute:ATK | 3* |
      | attribute:Class | Aggression |
      | attribute:Cost | 2 |
      | attribute:HP | 5 |
      | attribute:RES | Y |
      | attribute:Unique | 1 |

  @behavior:card:01051:printed-name
  @covers:behavior:card:01051:printed-subtitle
  @covers:behavior:card:01051:printed-type
  @covers:behavior:card:01051:printed-traits
  @covers:behavior:card:01051:printed-atk
  @covers:behavior:card:01051:printed-class
  @covers:behavior:card:01051:printed-cost
  @covers:behavior:card:01051:printed-hp
  @covers:behavior:card:01051:printed-res
  @covers:behavior:card:01051:printed-thw
  @covers:behavior:card:01051:printed-unique
  @card:01051
  Scenario: Card 01051 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 558 |
    When the printed characteristics of card 01051 copy 0 are requested
    Then card 01051 copy 0 exposes these printed characteristics
      | field | value |
      | name | Tigra |
      | subtitle | Greer Grant Nelson |
      | type | Ally |
      | traits | AVENGER |
      | attribute:ATK | 2* |
      | attribute:Class | Aggression |
      | attribute:Cost | 3 |
      | attribute:HP | 3 |
      | attribute:RES | B |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01052:printed-name
  @covers:behavior:card:01052:printed-type
  @covers:behavior:card:01052:printed-traits
  @covers:behavior:card:01052:printed-class
  @covers:behavior:card:01052:printed-cost
  @covers:behavior:card:01052:printed-res
  @card:01052
  Scenario: Card 01052 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 559 |
    When the printed characteristics of card 01052 copy 0 are requested
    Then card 01052 copy 0 exposes these printed characteristics
      | field | value |
      | name | Chase Them Down |
      | type | Event |
      | traits | THWART |
      | attribute:Class | Aggression |
      | attribute:Cost | 0 |
      | attribute:RES | B |

  @behavior:card:01053:printed-name
  @covers:behavior:card:01053:printed-type
  @covers:behavior:card:01053:printed-traits
  @covers:behavior:card:01053:printed-class
  @covers:behavior:card:01053:printed-cost
  @covers:behavior:card:01053:printed-res
  @card:01053
  Scenario: Card 01053 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 560 |
    When the printed characteristics of card 01053 copy 0 are requested
    Then card 01053 copy 0 exposes these printed characteristics
      | field | value |
      | name | Relentless Assault |
      | type | Event |
      | traits | ATTACK |
      | attribute:Class | Aggression |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01054:printed-name
  @covers:behavior:card:01054:printed-type
  @covers:behavior:card:01054:printed-traits
  @covers:behavior:card:01054:printed-class
  @covers:behavior:card:01054:printed-cost
  @covers:behavior:card:01054:printed-res
  @card:01054
  Scenario: Card 01054 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 561 |
    When the printed characteristics of card 01054 copy 0 are requested
    Then card 01054 copy 0 exposes these printed characteristics
      | field | value |
      | name | Uppercut |
      | type | Event |
      | traits | ATTACK |
      | attribute:Class | Aggression |
      | attribute:Cost | 3 |
      | attribute:RES | R |

  @behavior:card:01055:printed-name
  @covers:behavior:card:01055:printed-type
  @covers:behavior:card:01055:printed-class
  @covers:behavior:card:01055:printed-maxperdeck
  @covers:behavior:card:01055:printed-res
  @card:01055
  Scenario: Card 01055 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 562 |
    When the printed characteristics of card 01055 copy 0 are requested
    Then card 01055 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Power of Aggression |
      | type | Resource |
      | attribute:Class | Aggression |
      | attribute:MaxPerDeck | 2 |
      | attribute:RES | G |

  @behavior:card:01056:printed-name
  @covers:behavior:card:01056:printed-type
  @covers:behavior:card:01056:printed-traits
  @covers:behavior:card:01056:printed-class
  @covers:behavior:card:01056:printed-cost
  @covers:behavior:card:01056:printed-res
  @covers:behavior:card:01056:printed-uses
  @card:01056
  Scenario: Card 01056 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 563 |
    When the printed characteristics of card 01056 copy 0 are requested
    Then card 01056 copy 0 exposes these printed characteristics
      | field | value |
      | name | Tac Team |
      | type | Support |
      | traits | S.H.I.E.L.D |
      | attribute:Class | Aggression |
      | attribute:Cost | 3 |
      | attribute:RES | Y |
      | attribute:Uses | 3,attack |

  @behavior:card:01057:printed-name
  @covers:behavior:card:01057:printed-type
  @covers:behavior:card:01057:printed-traits
  @covers:behavior:card:01057:printed-class
  @covers:behavior:card:01057:printed-cost
  @covers:behavior:card:01057:printed-maxperunit
  @covers:behavior:card:01057:printed-maxperunitkind
  @covers:behavior:card:01057:printed-res
  @card:01057
  Scenario: Card 01057 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 564 |
    When the printed characteristics of card 01057 copy 0 are requested
    Then card 01057 copy 0 exposes these printed characteristics
      | field | value |
      | name | Combat Training |
      | type | Upgrade |
      | traits | SKILL |
      | attribute:Class | Aggression |
      | attribute:Cost | 2 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | R |

  @behavior:card:01058:printed-name
  @covers:behavior:card:01058:printed-subtitle
  @covers:behavior:card:01058:printed-type
  @covers:behavior:card:01058:printed-traits
  @covers:behavior:card:01058:printed-atk
  @covers:behavior:card:01058:printed-class
  @covers:behavior:card:01058:printed-cost
  @covers:behavior:card:01058:printed-hp
  @covers:behavior:card:01058:printed-res
  @covers:behavior:card:01058:printed-thw
  @covers:behavior:card:01058:printed-unique
  @card:01058
  Scenario: Card 01058 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 565 |
    When the printed characteristics of card 01058 copy 0 are requested
    Then card 01058 copy 0 exposes these printed characteristics
      | field | value |
      | name | Daredevil |
      | subtitle | Matt Murdock |
      | type | Ally |
      | traits | DEFENDER |
      | attribute:ATK | 2* |
      | attribute:Class | Justice |
      | attribute:Cost | 4 |
      | attribute:HP | 3 |
      | attribute:RES | R |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01059:printed-name
  @covers:behavior:card:01059:printed-type
  @covers:behavior:card:01059:printed-traits
  @covers:behavior:card:01059:printed-atk
  @covers:behavior:card:01059:printed-class
  @covers:behavior:card:01059:printed-cost
  @covers:behavior:card:01059:printed-hp
  @covers:behavior:card:01059:printed-res
  @covers:behavior:card:01059:printed-thw
  @covers:behavior:card:01059:printed-unique
  @card:01059
  Scenario: Card 01059 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 566 |
    When the printed characteristics of card 01059 copy 0 are requested
    Then card 01059 copy 0 exposes these printed characteristics
      | field | value |
      | name | Jessica Jones |
      | type | Ally |
      | traits | DEFENDER |
      | attribute:ATK | 2* |
      | attribute:Class | Justice |
      | attribute:Cost | 3 |
      | attribute:HP | 3 |
      | attribute:RES | Y |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01060:printed-name
  @covers:behavior:card:01060:printed-type
  @covers:behavior:card:01060:printed-traits
  @covers:behavior:card:01060:printed-class
  @covers:behavior:card:01060:printed-cost
  @covers:behavior:card:01060:printed-res
  @card:01060
  Scenario: Card 01060 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 567 |
    When the printed characteristics of card 01060 copy 0 are requested
    Then card 01060 copy 0 exposes these printed characteristics
      | field | value |
      | name | For Justice! |
      | type | Event |
      | traits | THWART |
      | attribute:Class | Justice |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01061:printed-name
  @covers:behavior:card:01061:printed-type
  @covers:behavior:card:01061:printed-class
  @covers:behavior:card:01061:printed-cost
  @covers:behavior:card:01061:printed-res
  @card:01061
  Scenario: Card 01061 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 568 |
    When the printed characteristics of card 01061 copy 0 are requested
    Then card 01061 copy 0 exposes these printed characteristics
      | field | value |
      | name | Great Responsibility |
      | type | Event |
      | attribute:Class | Justice |
      | attribute:Cost | 0 |
      | attribute:RES | B |

  @behavior:card:01062:printed-name
  @covers:behavior:card:01062:printed-type
  @covers:behavior:card:01062:printed-class
  @covers:behavior:card:01062:printed-maxperdeck
  @covers:behavior:card:01062:printed-res
  @card:01062
  Scenario: Card 01062 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 569 |
    When the printed characteristics of card 01062 copy 0 are requested
    Then card 01062 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Power of Justice |
      | type | Resource |
      | attribute:Class | Justice |
      | attribute:MaxPerDeck | 2 |
      | attribute:RES | G |

  @behavior:card:01063:printed-name
  @covers:behavior:card:01063:printed-type
  @covers:behavior:card:01063:printed-traits
  @covers:behavior:card:01063:printed-class
  @covers:behavior:card:01063:printed-cost
  @covers:behavior:card:01063:printed-maxperunit
  @covers:behavior:card:01063:printed-maxperunitkind
  @covers:behavior:card:01063:printed-res
  @card:01063
  Scenario: Card 01063 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 570 |
    When the printed characteristics of card 01063 copy 0 are requested
    Then card 01063 copy 0 exposes these printed characteristics
      | field | value |
      | name | Interrogation Room |
      | type | Support |
      | traits | LOCATION |
      | attribute:Class | Justice |
      | attribute:Cost | 1 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | Y |

  @behavior:card:01064:printed-name
  @covers:behavior:card:01064:printed-type
  @covers:behavior:card:01064:printed-traits
  @covers:behavior:card:01064:printed-class
  @covers:behavior:card:01064:printed-cost
  @covers:behavior:card:01064:printed-res
  @covers:behavior:card:01064:printed-uses
  @card:01064
  Scenario: Card 01064 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 571 |
    When the printed characteristics of card 01064 copy 0 are requested
    Then card 01064 copy 0 exposes these printed characteristics
      | field | value |
      | name | Surveillance Team |
      | type | Support |
      | traits | S.H.I.E.L.D |
      | attribute:Class | Justice |
      | attribute:Cost | 2 |
      | attribute:RES | B |
      | attribute:Uses | 3,snoop |

  @behavior:card:01065:printed-name
  @covers:behavior:card:01065:printed-type
  @covers:behavior:card:01065:printed-traits
  @covers:behavior:card:01065:printed-class
  @covers:behavior:card:01065:printed-cost
  @covers:behavior:card:01065:printed-maxperunit
  @covers:behavior:card:01065:printed-maxperunitkind
  @covers:behavior:card:01065:printed-res
  @card:01065
  Scenario: Card 01065 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 572 |
    When the printed characteristics of card 01065 copy 0 are requested
    Then card 01065 copy 0 exposes these printed characteristics
      | field | value |
      | name | Heroic Intuition |
      | type | Upgrade |
      | traits | SKILL |
      | attribute:Class | Justice |
      | attribute:Cost | 2 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | Y |

  @behavior:card:01066:printed-name
  @covers:behavior:card:01066:printed-subtitle
  @covers:behavior:card:01066:printed-type
  @covers:behavior:card:01066:printed-traits
  @covers:behavior:card:01066:printed-atk
  @covers:behavior:card:01066:printed-class
  @covers:behavior:card:01066:printed-cost
  @covers:behavior:card:01066:printed-hp
  @covers:behavior:card:01066:printed-res
  @covers:behavior:card:01066:printed-thw
  @covers:behavior:card:01066:printed-unique
  @card:01066
  Scenario: Card 01066 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 573 |
    When the printed characteristics of card 01066 copy 0 are requested
    Then card 01066 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hawkeye |
      | subtitle | Clint Barton |
      | type | Ally |
      | traits | AVENGER |
      | attribute:ATK | 1* |
      | attribute:Class | Leadership |
      | attribute:Cost | 3 |
      | attribute:HP | 3 |
      | attribute:RES | Y |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01067:printed-name
  @covers:behavior:card:01067:printed-type
  @covers:behavior:card:01067:printed-traits
  @covers:behavior:card:01067:printed-atk
  @covers:behavior:card:01067:printed-class
  @covers:behavior:card:01067:printed-cost
  @covers:behavior:card:01067:printed-hp
  @covers:behavior:card:01067:printed-res
  @covers:behavior:card:01067:printed-thw
  @covers:behavior:card:01067:printed-unique
  @card:01067
  Scenario: Card 01067 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 574 |
    When the printed characteristics of card 01067 copy 0 are requested
    Then card 01067 copy 0 exposes these printed characteristics
      | field | value |
      | name | Maria Hill |
      | type | Ally |
      | traits | S.H.I.E.L.D |
      | attribute:ATK | 1* |
      | attribute:Class | Leadership |
      | attribute:Cost | 2 |
      | attribute:HP | 2 |
      | attribute:RES | B |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01068:printed-name
  @covers:behavior:card:01068:printed-type
  @covers:behavior:card:01068:printed-traits
  @covers:behavior:card:01068:printed-atk
  @covers:behavior:card:01068:printed-class
  @covers:behavior:card:01068:printed-cost
  @covers:behavior:card:01068:printed-hp
  @covers:behavior:card:01068:printed-res
  @covers:behavior:card:01068:printed-thw
  @covers:behavior:card:01068:printed-unique
  @card:01068
  Scenario: Card 01068 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 575 |
    When the printed characteristics of card 01068 copy 0 are requested
    Then card 01068 copy 0 exposes these printed characteristics
      | field | value |
      | name | Vision |
      | type | Ally |
      | traits | ANDROID/AVENGER |
      | attribute:ATK | 2* |
      | attribute:Class | Leadership |
      | attribute:Cost | 4 |
      | attribute:HP | 3 |
      | attribute:RES | R |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01069:printed-name
  @covers:behavior:card:01069:printed-type
  @covers:behavior:card:01069:printed-class
  @covers:behavior:card:01069:printed-cost
  @covers:behavior:card:01069:printed-res
  @card:01069
  Scenario: Card 01069 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 576 |
    When the printed characteristics of card 01069 copy 0 are requested
    Then card 01069 copy 0 exposes these printed characteristics
      | field | value |
      | name | Get Ready |
      | type | Event |
      | attribute:Class | Leadership |
      | attribute:Cost | 0 |
      | attribute:RES | R |

  @behavior:card:01070:printed-name
  @covers:behavior:card:01070:printed-type
  @covers:behavior:card:01070:printed-traits
  @covers:behavior:card:01070:printed-class
  @covers:behavior:card:01070:printed-cost
  @covers:behavior:card:01070:printed-res
  @card:01070
  Scenario: Card 01070 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 577 |
    When the printed characteristics of card 01070 copy 0 are requested
    Then card 01070 copy 0 exposes these printed characteristics
      | field | value |
      | name | Lead from the Front |
      | type | Event |
      | traits | TACTIC |
      | attribute:Class | Leadership |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01071:printed-name
  @covers:behavior:card:01071:printed-type
  @covers:behavior:card:01071:printed-class
  @covers:behavior:card:01071:printed-cost
  @covers:behavior:card:01071:printed-res
  @card:01071
  Scenario: Card 01071 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 578 |
    When the printed characteristics of card 01071 copy 0 are requested
    Then card 01071 copy 0 exposes these printed characteristics
      | field | value |
      | name | Make the Call |
      | type | Event |
      | attribute:Class | Leadership |
      | attribute:Cost | 0 |
      | attribute:RES | B |

  @behavior:card:01072:printed-name
  @covers:behavior:card:01072:printed-type
  @covers:behavior:card:01072:printed-class
  @covers:behavior:card:01072:printed-maxperdeck
  @covers:behavior:card:01072:printed-res
  @card:01072
  Scenario: Card 01072 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 579 |
    When the printed characteristics of card 01072 copy 0 are requested
    Then card 01072 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Power of Leadership |
      | type | Resource |
      | attribute:Class | Leadership |
      | attribute:MaxPerDeck | 2 |
      | attribute:RES | G |

  @behavior:card:01073:printed-name
  @covers:behavior:card:01073:printed-type
  @covers:behavior:card:01073:printed-traits
  @covers:behavior:card:01073:printed-class
  @covers:behavior:card:01073:printed-cost
  @covers:behavior:card:01073:printed-res
  @covers:behavior:card:01073:printed-unique
  @card:01073
  Scenario: Card 01073 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 580 |
    When the printed characteristics of card 01073 copy 0 are requested
    Then card 01073 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Triskelion |
      | type | Support |
      | traits | LOCATION/S.H.I.E.L.D |
      | attribute:Class | Leadership |
      | attribute:Cost | 1 |
      | attribute:RES | Y |
      | attribute:Unique | 1 |

  @behavior:card:01074:printed-name
  @covers:behavior:card:01074:printed-type
  @covers:behavior:card:01074:printed-traits
  @covers:behavior:card:01074:printed-class
  @covers:behavior:card:01074:printed-cost
  @covers:behavior:card:01074:printed-maxperunit
  @covers:behavior:card:01074:printed-maxperunitkind
  @covers:behavior:card:01074:printed-res
  @card:01074
  Scenario: Card 01074 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 581 |
    When the printed characteristics of card 01074 copy 0 are requested
    Then card 01074 copy 0 exposes these printed characteristics
      | field | value |
      | name | Inspired |
      | type | Upgrade |
      | traits | CONDITION |
      | attribute:Class | Leadership |
      | attribute:Cost | 1 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | ally |
      | attribute:RES | R |

  @behavior:card:01075:printed-name
  @covers:behavior:card:01075:printed-subtitle
  @covers:behavior:card:01075:printed-type
  @covers:behavior:card:01075:printed-traits
  @covers:behavior:card:01075:printed-atk
  @covers:behavior:card:01075:printed-class
  @covers:behavior:card:01075:printed-cost
  @covers:behavior:card:01075:printed-hp
  @covers:behavior:card:01075:printed-res
  @covers:behavior:card:01075:printed-thw
  @covers:behavior:card:01075:printed-unique
  @card:01075
  Scenario: Card 01075 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 582 |
    When the printed characteristics of card 01075 copy 0 are requested
    Then card 01075 copy 0 exposes these printed characteristics
      | field | value |
      | name | Black Widow |
      | subtitle | Natasha Romanoff |
      | type | Ally |
      | traits | S.H.I.E.L.D/SPY |
      | attribute:ATK | 1* |
      | attribute:Class | Protection |
      | attribute:Cost | 3 |
      | attribute:HP | 2 |
      | attribute:RES | R |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01076:printed-name
  @covers:behavior:card:01076:printed-type
  @covers:behavior:card:01076:printed-traits
  @covers:behavior:card:01076:printed-atk
  @covers:behavior:card:01076:printed-class
  @covers:behavior:card:01076:printed-cost
  @covers:behavior:card:01076:printed-hp
  @covers:behavior:card:01076:printed-res
  @covers:behavior:card:01076:printed-thw
  @covers:behavior:card:01076:printed-toughness
  @covers:behavior:card:01076:printed-unique
  @card:01076
  Scenario: Card 01076 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 583 |
    When the printed characteristics of card 01076 copy 0 are requested
    Then card 01076 copy 0 exposes these printed characteristics
      | field | value |
      | name | Luke Cage |
      | type | Ally |
      | traits | DEFENDER |
      | attribute:ATK | 2* |
      | attribute:Class | Protection |
      | attribute:Cost | 4 |
      | attribute:HP | 5 |
      | attribute:RES | Y |
      | attribute:THW | 1* |
      | attribute:Toughness | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01077:printed-name
  @covers:behavior:card:01077:printed-type
  @covers:behavior:card:01077:printed-traits
  @covers:behavior:card:01077:printed-class
  @covers:behavior:card:01077:printed-cost
  @covers:behavior:card:01077:printed-res
  @card:01077
  Scenario: Card 01077 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 584 |
    When the printed characteristics of card 01077 copy 0 are requested
    Then card 01077 copy 0 exposes these printed characteristics
      | field | value |
      | name | Counter-Punch |
      | type | Event |
      | traits | ATTACK |
      | attribute:Class | Protection |
      | attribute:Cost | 0 |
      | attribute:RES | R |

  @behavior:card:01078:printed-name
  @covers:behavior:card:01078:printed-type
  @covers:behavior:card:01078:printed-class
  @covers:behavior:card:01078:printed-cost
  @covers:behavior:card:01078:printed-res
  @card:01078
  Scenario: Card 01078 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 585 |
    When the printed characteristics of card 01078 copy 0 are requested
    Then card 01078 copy 0 exposes these printed characteristics
      | field | value |
      | name | Get Behind Me! |
      | type | Event |
      | attribute:Class | Protection |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01079:printed-name
  @covers:behavior:card:01079:printed-type
  @covers:behavior:card:01079:printed-class
  @covers:behavior:card:01079:printed-maxperdeck
  @covers:behavior:card:01079:printed-res
  @card:01079
  Scenario: Card 01079 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 586 |
    When the printed characteristics of card 01079 copy 0 are requested
    Then card 01079 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Power of Protection |
      | type | Resource |
      | attribute:Class | Protection |
      | attribute:MaxPerDeck | 2 |
      | attribute:RES | G |

  @behavior:card:01080:printed-name
  @covers:behavior:card:01080:printed-type
  @covers:behavior:card:01080:printed-traits
  @covers:behavior:card:01080:printed-class
  @covers:behavior:card:01080:printed-cost
  @covers:behavior:card:01080:printed-res
  @covers:behavior:card:01080:printed-uses
  @card:01080
  Scenario: Card 01080 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 587 |
    When the printed characteristics of card 01080 copy 0 are requested
    Then card 01080 copy 0 exposes these printed characteristics
      | field | value |
      | name | Med Team |
      | type | Support |
      | traits | S.H.I.E.L.D |
      | attribute:Class | Protection |
      | attribute:Cost | 3 |
      | attribute:RES | Y |
      | attribute:Uses | 3,medical |

  @behavior:card:01081:printed-name
  @covers:behavior:card:01081:printed-type
  @covers:behavior:card:01081:printed-traits
  @covers:behavior:card:01081:printed-class
  @covers:behavior:card:01081:printed-cost
  @covers:behavior:card:01081:printed-maxperunit
  @covers:behavior:card:01081:printed-maxperunitkind
  @covers:behavior:card:01081:printed-res
  @card:01081
  Scenario: Card 01081 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 588 |
    When the printed characteristics of card 01081 copy 0 are requested
    Then card 01081 copy 0 exposes these printed characteristics
      | field | value |
      | name | Armored Vest |
      | type | Upgrade |
      | traits | ARMOR |
      | attribute:Class | Protection |
      | attribute:Cost | 1 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | B |

  @behavior:card:01082:printed-name
  @covers:behavior:card:01082:printed-type
  @covers:behavior:card:01082:printed-traits
  @covers:behavior:card:01082:printed-class
  @covers:behavior:card:01082:printed-cost
  @covers:behavior:card:01082:printed-res
  @card:01082
  Scenario: Card 01082 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 589 |
    When the printed characteristics of card 01082 copy 0 are requested
    Then card 01082 copy 0 exposes these printed characteristics
      | field | value |
      | name | Indomitable |
      | type | Upgrade |
      | traits | CONDITION |
      | attribute:Class | Protection |
      | attribute:Cost | 1 |
      | attribute:RES | Y |

  @behavior:card:01083:printed-name
  @covers:behavior:card:01083:printed-subtitle
  @covers:behavior:card:01083:printed-type
  @covers:behavior:card:01083:printed-traits
  @covers:behavior:card:01083:printed-atk
  @covers:behavior:card:01083:printed-class
  @covers:behavior:card:01083:printed-cost
  @covers:behavior:card:01083:printed-hp
  @covers:behavior:card:01083:printed-res
  @covers:behavior:card:01083:printed-thw
  @covers:behavior:card:01083:printed-unique
  @card:01083
  Scenario: Card 01083 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 590 |
    When the printed characteristics of card 01083 copy 0 are requested
    Then card 01083 copy 0 exposes these printed characteristics
      | field | value |
      | name | Mockingbird |
      | subtitle | Bobbi Morse |
      | type | Ally |
      | traits | S.H.I.E.L.D/SPY |
      | attribute:ATK | 1* |
      | attribute:Class | Basic |
      | attribute:Cost | 3 |
      | attribute:HP | 3 |
      | attribute:RES | R |
      | attribute:THW | 1* |
      | attribute:Unique | 1 |

  @behavior:card:01084:printed-name
  @covers:behavior:card:01084:printed-type
  @covers:behavior:card:01084:printed-traits
  @covers:behavior:card:01084:printed-atk
  @covers:behavior:card:01084:printed-class
  @covers:behavior:card:01084:printed-cost
  @covers:behavior:card:01084:printed-hp
  @covers:behavior:card:01084:printed-res
  @covers:behavior:card:01084:printed-thw
  @covers:behavior:card:01084:printed-unique
  @card:01084
  Scenario: Card 01084 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 591 |
    When the printed characteristics of card 01084 copy 0 are requested
    Then card 01084 copy 0 exposes these printed characteristics
      | field | value |
      | name | Nick Fury |
      | type | Ally |
      | traits | S.H.I.E.L.D/SPY |
      | attribute:ATK | 2* |
      | attribute:Class | Basic |
      | attribute:Cost | 4 |
      | attribute:HP | 3 |
      | attribute:RES | B |
      | attribute:THW | 2* |
      | attribute:Unique | 1 |

  @behavior:card:01085:printed-name
  @covers:behavior:card:01085:printed-type
  @covers:behavior:card:01085:printed-traits
  @covers:behavior:card:01085:printed-class
  @covers:behavior:card:01085:printed-cost
  @covers:behavior:card:01085:printed-res
  @card:01085
  Scenario: Card 01085 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 592 |
    When the printed characteristics of card 01085 copy 0 are requested
    Then card 01085 copy 0 exposes these printed characteristics
      | field | value |
      | name | Emergency |
      | type | Event |
      | traits | THWART |
      | attribute:Class | Basic |
      | attribute:Cost | 0 |
      | attribute:RES | Y |

  @behavior:card:01086:printed-name
  @covers:behavior:card:01086:printed-type
  @covers:behavior:card:01086:printed-class
  @covers:behavior:card:01086:printed-cost
  @covers:behavior:card:01086:printed-res
  @card:01086
  Scenario: Card 01086 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 593 |
    When the printed characteristics of card 01086 copy 0 are requested
    Then card 01086 copy 0 exposes these printed characteristics
      | field | value |
      | name | First Aid |
      | type | Event |
      | attribute:Class | Basic |
      | attribute:Cost | 1 |
      | attribute:RES | B |

  @behavior:card:01087:printed-name
  @covers:behavior:card:01087:printed-type
  @covers:behavior:card:01087:printed-traits
  @covers:behavior:card:01087:printed-class
  @covers:behavior:card:01087:printed-cost
  @covers:behavior:card:01087:printed-res
  @card:01087
  Scenario: Card 01087 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 594 |
    When the printed characteristics of card 01087 copy 0 are requested
    Then card 01087 copy 0 exposes these printed characteristics
      | field | value |
      | name | Haymaker |
      | type | Event |
      | traits | ATTACK |
      | attribute:Class | Basic |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01088:printed-name
  @covers:behavior:card:01088:printed-type
  @covers:behavior:card:01088:printed-class
  @covers:behavior:card:01088:printed-maxperdeck
  @covers:behavior:card:01088:printed-res
  @card:01088
  Scenario: Card 01088 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 595 |
    When the printed characteristics of card 01088 copy 0 are requested
    Then card 01088 copy 0 exposes these printed characteristics
      | field | value |
      | name | Energy |
      | type | Resource |
      | attribute:Class | Basic |
      | attribute:MaxPerDeck | 1 |
      | attribute:RES | YY |

  @behavior:card:01089:printed-name
  @covers:behavior:card:01089:printed-type
  @covers:behavior:card:01089:printed-class
  @covers:behavior:card:01089:printed-maxperdeck
  @covers:behavior:card:01089:printed-res
  @card:01089
  Scenario: Card 01089 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 596 |
    When the printed characteristics of card 01089 copy 0 are requested
    Then card 01089 copy 0 exposes these printed characteristics
      | field | value |
      | name | Genius |
      | type | Resource |
      | attribute:Class | Basic |
      | attribute:MaxPerDeck | 1 |
      | attribute:RES | BB |

  @behavior:card:01090:printed-name
  @covers:behavior:card:01090:printed-type
  @covers:behavior:card:01090:printed-class
  @covers:behavior:card:01090:printed-maxperdeck
  @covers:behavior:card:01090:printed-res
  @card:01090
  Scenario: Card 01090 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 597 |
    When the printed characteristics of card 01090 copy 0 are requested
    Then card 01090 copy 0 exposes these printed characteristics
      | field | value |
      | name | Strength |
      | type | Resource |
      | attribute:Class | Basic |
      | attribute:MaxPerDeck | 1 |
      | attribute:RES | RR |

  @behavior:card:01091:printed-name
  @covers:behavior:card:01091:printed-type
  @covers:behavior:card:01091:printed-traits
  @covers:behavior:card:01091:printed-class
  @covers:behavior:card:01091:printed-cost
  @covers:behavior:card:01091:printed-maxperunit
  @covers:behavior:card:01091:printed-maxperunitkind
  @covers:behavior:card:01091:printed-res
  @card:01091
  Scenario: Card 01091 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 598 |
    When the printed characteristics of card 01091 copy 0 are requested
    Then card 01091 copy 0 exposes these printed characteristics
      | field | value |
      | name | Avengers Mansion |
      | type | Support |
      | traits | AVENGER/LOCATION |
      | attribute:Class | Basic |
      | attribute:Cost | 4 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | B |

  @behavior:card:01092:printed-name
  @covers:behavior:card:01092:printed-type
  @covers:behavior:card:01092:printed-traits
  @covers:behavior:card:01092:printed-class
  @covers:behavior:card:01092:printed-cost
  @covers:behavior:card:01092:printed-maxperunit
  @covers:behavior:card:01092:printed-maxperunitkind
  @covers:behavior:card:01092:printed-res
  @card:01092
  Scenario: Card 01092 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 599 |
    When the printed characteristics of card 01092 copy 0 are requested
    Then card 01092 copy 0 exposes these printed characteristics
      | field | value |
      | name | Helicarrier |
      | type | Support |
      | traits | LOCATION/S.H.I.E.L.D |
      | attribute:Class | Basic |
      | attribute:Cost | 3 |
      | attribute:MaxPerUnit | 1 |
      | attribute:MaxPerUnitKind | player |
      | attribute:RES | R |

  @behavior:card:01093:printed-name
  @covers:behavior:card:01093:printed-type
  @covers:behavior:card:01093:printed-traits
  @covers:behavior:card:01093:printed-class
  @covers:behavior:card:01093:printed-cost
  @covers:behavior:card:01093:printed-res
  @card:01093
  Scenario: Card 01093 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 600 |
    When the printed characteristics of card 01093 copy 0 are requested
    Then card 01093 copy 0 exposes these printed characteristics
      | field | value |
      | name | Tenacity |
      | type | Upgrade |
      | traits | CONDITION |
      | attribute:Class | Basic |
      | attribute:Cost | 2 |
      | attribute:RES | Y |

  @behavior:card:01094:printed-name
  @covers:behavior:card:01094:printed-type
  @covers:behavior:card:01094:printed-traits
  @covers:behavior:card:01094:printed-atk
  @covers:behavior:card:01094:printed-hp
  @covers:behavior:card:01094:printed-sch
  @covers:behavior:card:01094:printed-stage
  @covers:behavior:card:01094:printed-unique
  @card:01094
  Scenario: Card 01094 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 601 |
    When the printed characteristics of card 01094 copy 0 are requested
    Then card 01094 copy 0 exposes these printed characteristics
      | field | value |
      | name | Rhino |
      | type | Villain |
      | traits | BRUTE/CRIMINAL |
      | attribute:ATK | 2 |
      | attribute:HP | 14* |
      | attribute:SCH | 1 |
      | attribute:Stage | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01095:printed-name
  @covers:behavior:card:01095:printed-type
  @covers:behavior:card:01095:printed-traits
  @covers:behavior:card:01095:printed-atk
  @covers:behavior:card:01095:printed-hp
  @covers:behavior:card:01095:printed-sch
  @covers:behavior:card:01095:printed-stage
  @covers:behavior:card:01095:printed-unique
  @card:01095
  Scenario: Card 01095 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 602 |
    When the printed characteristics of card 01095 copy 0 are requested
    Then card 01095 copy 0 exposes these printed characteristics
      | field | value |
      | name | Rhino |
      | type | Villain |
      | traits | BRUTE/CRIMINAL |
      | attribute:ATK | 3 |
      | attribute:HP | 15* |
      | attribute:SCH | 1 |
      | attribute:Stage | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01096:printed-name
  @covers:behavior:card:01096:printed-type
  @covers:behavior:card:01096:printed-traits
  @covers:behavior:card:01096:printed-atk
  @covers:behavior:card:01096:printed-hp
  @covers:behavior:card:01096:printed-sch
  @covers:behavior:card:01096:printed-stage
  @covers:behavior:card:01096:printed-toughness
  @covers:behavior:card:01096:printed-unique
  @card:01096
  Scenario: Card 01096 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino_expert | spider_man | 603 |
    When the printed characteristics of card 01096 copy 0 are requested
    Then card 01096 copy 0 exposes these printed characteristics
      | field | value |
      | name | Rhino |
      | type | Villain |
      | traits | BRUTE/CRIMINAL |
      | attribute:ATK | 4 |
      | attribute:HP | 16* |
      | attribute:SCH | 1 |
      | attribute:Stage | 3 |
      | attribute:Toughness | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01097a:printed-name
  @covers:behavior:card:01097a:printed-type
  @covers:behavior:card:01097a:printed-stage
  @card:01097a
  Scenario: Card 01097a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 604 |
    When the printed characteristics of card 01097a copy 0 are requested
    Then card 01097a copy 0 exposes these printed characteristics
      | field | value |
      | name | The Break-In! |
      | type | MainScheme |
      | attribute:Stage | 1 |

  @behavior:card:01097b:printed-name
  @covers:behavior:card:01097b:printed-type
  @covers:behavior:card:01097b:printed-escalationthreat
  @covers:behavior:card:01097b:printed-stage
  @covers:behavior:card:01097b:printed-startingthreat
  @covers:behavior:card:01097b:printed-targetthreat
  @card:01097b
  Scenario: Card 01097b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 605 |
    When the printed characteristics of card 01097b copy 0 are requested
    Then card 01097b copy 0 exposes these printed characteristics
      | field | value |
      | name | The Break-In! |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 1 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 7* |

  @behavior:card:01098:printed-name
  @covers:behavior:card:01098:printed-type
  @covers:behavior:card:01098:printed-traits
  @card:01098
  Scenario: Card 01098 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 606 |
    When the printed characteristics of card 01098 copy 0 are requested
    Then card 01098 copy 0 exposes these printed characteristics
      | field | value |
      | name | Armored Rhino Suit |
      | type | Attachment |
      | traits | ARMOR |

  @behavior:card:01099:printed-name
  @covers:behavior:card:01099:printed-type
  @covers:behavior:card:01099:printed-atk
  @covers:behavior:card:01099:printed-boost
  @card:01099
  Scenario: Card 01099 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 607 |
    When the printed characteristics of card 01099 copy 0 are requested
    Then card 01099 copy 0 exposes these printed characteristics
      | field | value |
      | name | Charge |
      | type | Attachment |
      | attribute:ATK+ | 3 |
      | attribute:Boost | 2 |

  @behavior:card:01100:printed-name
  @covers:behavior:card:01100:printed-type
  @covers:behavior:card:01100:printed-traits
  @covers:behavior:card:01100:printed-atk
  @covers:behavior:card:01100:printed-boost
  @card:01100
  Scenario: Card 01100 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 608 |
    When the printed characteristics of card 01100 copy 0 are requested
    Then card 01100 copy 0 exposes these printed characteristics
      | field | value |
      | name | Enhanced Ivory Horn |
      | type | Attachment |
      | traits | WEAPON |
      | attribute:ATK+ | 1 |
      | attribute:Boost | 2 |

  @behavior:card:01101:printed-name
  @covers:behavior:card:01101:printed-type
  @covers:behavior:card:01101:printed-traits
  @covers:behavior:card:01101:printed-atk
  @covers:behavior:card:01101:printed-boost
  @covers:behavior:card:01101:printed-guard
  @covers:behavior:card:01101:printed-hp
  @covers:behavior:card:01101:printed-sch
  @card:01101
  Scenario: Card 01101 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 609 |
    When the printed characteristics of card 01101 copy 0 are requested
    Then card 01101 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hydra Mercenary |
      | type | Minion |
      | traits | HYDRA |
      | attribute:ATK | 1 |
      | attribute:Boost | 1 |
      | attribute:Guard | 1 |
      | attribute:HP | 3 |
      | attribute:SCH | 0 |

  @behavior:card:01102:printed-name
  @covers:behavior:card:01102:printed-type
  @covers:behavior:card:01102:printed-traits
  @covers:behavior:card:01102:printed-atk
  @covers:behavior:card:01102:printed-boost
  @covers:behavior:card:01102:printed-hp
  @covers:behavior:card:01102:printed-sch
  @covers:behavior:card:01102:printed-toughness
  @covers:behavior:card:01102:printed-unique
  @card:01102
  Scenario: Card 01102 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 610 |
    When the printed characteristics of card 01102 copy 0 are requested
    Then card 01102 copy 0 exposes these printed characteristics
      | field | value |
      | name | Sandman |
      | type | Minion |
      | traits | CRIMINAL/ELITE |
      | attribute:ATK | 3 |
      | attribute:Boost | 2 |
      | attribute:HP | 4 |
      | attribute:SCH | 2 |
      | attribute:Toughness | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01103:printed-name
  @covers:behavior:card:01103:printed-type
  @covers:behavior:card:01103:printed-traits
  @covers:behavior:card:01103:printed-atk
  @covers:behavior:card:01103:printed-boost
  @covers:behavior:card:01103:printed-hp
  @covers:behavior:card:01103:printed-sch
  @covers:behavior:card:01103:printed-unique
  @card:01103
  Scenario: Card 01103 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 611 |
    When the printed characteristics of card 01103 copy 0 are requested
    Then card 01103 copy 0 exposes these printed characteristics
      | field | value |
      | name | Shocker |
      | type | Minion |
      | traits | CRIMINAL |
      | attribute:ATK | 2 |
      | attribute:Boost | 2 |
      | attribute:HP | 3 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01104:printed-name
  @covers:behavior:card:01104:printed-type
  @card:01104
  Scenario: Card 01104 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 612 |
    When the printed characteristics of card 01104 copy 0 are requested
    Then card 01104 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hard to Keep Down |
      | type | Treachery |

  @behavior:card:01105:printed-name
  @covers:behavior:card:01105:printed-type
  @card:01105
  Scenario: Card 01105 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 613 |
    When the printed characteristics of card 01105 copy 0 are requested
    Then card 01105 copy 0 exposes these printed characteristics
      | field | value |
      | name | "I'm Tough" |
      | type | Treachery |

  @behavior:card:01106:printed-name
  @covers:behavior:card:01106:printed-type
  @covers:behavior:card:01106:printed-boost
  @card:01106
  Scenario: Card 01106 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 614 |
    When the printed characteristics of card 01106 copy 0 are requested
    Then card 01106 copy 0 exposes these printed characteristics
      | field | value |
      | name | Stampede |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01107:printed-name
  @covers:behavior:card:01107:printed-type
  @covers:behavior:card:01107:printed-boost
  @covers:behavior:card:01107:printed-hazard
  @covers:behavior:card:01107:printed-startingthreat
  @card:01107
  Scenario: Card 01107 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 615 |
    When the printed characteristics of card 01107 copy 0 are requested
    Then card 01107 copy 0 exposes these printed characteristics
      | field | value |
      | name | Breakin' & Takin' |
      | type | SideScheme |
      | attribute:Boost | 2 |
      | attribute:Hazard | 1 |
      | attribute:StartingThreat | 2 |

  @behavior:card:01108:printed-name
  @covers:behavior:card:01108:printed-type
  @covers:behavior:card:01108:printed-boost
  @covers:behavior:card:01108:printed-crisis
  @covers:behavior:card:01108:printed-startingthreat
  @card:01108
  Scenario: Card 01108 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 616 |
    When the printed characteristics of card 01108 copy 0 are requested
    Then card 01108 copy 0 exposes these printed characteristics
      | field | value |
      | name | Crowd Control |
      | type | SideScheme |
      | attribute:Boost | 2 |
      | attribute:Crisis | 1 |
      | attribute:StartingThreat | 2* |

  @behavior:card:01109:printed-name
  @covers:behavior:card:01109:printed-type
  @covers:behavior:card:01109:printed-acceleration
  @covers:behavior:card:01109:printed-boost
  @covers:behavior:card:01109:printed-startingthreat
  @card:01109
  Scenario: Card 01109 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 617 |
    When the printed characteristics of card 01109 copy 0 are requested
    Then card 01109 copy 0 exposes these printed characteristics
      | field | value |
      | name | Bomb Scare |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 2 |
      | attribute:StartingThreat | 2 |

  @behavior:card:01110:printed-name
  @covers:behavior:card:01110:printed-type
  @covers:behavior:card:01110:printed-traits
  @covers:behavior:card:01110:printed-atk
  @covers:behavior:card:01110:printed-boost
  @covers:behavior:card:01110:printed-hp
  @covers:behavior:card:01110:printed-sch
  @card:01110
  Scenario: Card 01110 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 618 |
    When the printed characteristics of card 01110 copy 0 are requested
    Then card 01110 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hydra Bomber |
      | type | Minion |
      | traits | HYDRA |
      | attribute:ATK | 1 |
      | attribute:Boost | 1 |
      | attribute:HP | 2 |
      | attribute:SCH | 1 |

  @behavior:card:01111:printed-name
  @covers:behavior:card:01111:printed-type
  @covers:behavior:card:01111:printed-boost
  @card:01111
  Scenario: Card 01111 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 619 |
    When the printed characteristics of card 01111 copy 0 are requested
    Then card 01111 copy 0 exposes these printed characteristics
      | field | value |
      | name | Explosion |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01112:printed-name
  @covers:behavior:card:01112:printed-type
  @covers:behavior:card:01112:printed-boost
  @card:01112
  Scenario: Card 01112 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 620 |
    When the printed characteristics of card 01112 copy 0 are requested
    Then card 01112 copy 0 exposes these printed characteristics
      | field | value |
      | name | False Alarm |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01113:printed-name
  @covers:behavior:card:01113:printed-type
  @covers:behavior:card:01113:printed-traits
  @covers:behavior:card:01113:printed-atk
  @covers:behavior:card:01113:printed-hp
  @covers:behavior:card:01113:printed-sch
  @covers:behavior:card:01113:printed-stage
  @covers:behavior:card:01113:printed-unique
  @card:01113
  Scenario: Card 01113 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 621 |
    When the printed characteristics of card 01113 copy 0 are requested
    Then card 01113 copy 0 exposes these printed characteristics
      | field | value |
      | name | Klaw |
      | type | Villain |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 0 |
      | attribute:HP | 12* |
      | attribute:SCH | 2 |
      | attribute:Stage | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01114:printed-name
  @covers:behavior:card:01114:printed-type
  @covers:behavior:card:01114:printed-traits
  @covers:behavior:card:01114:printed-atk
  @covers:behavior:card:01114:printed-hp
  @covers:behavior:card:01114:printed-sch
  @covers:behavior:card:01114:printed-stage
  @covers:behavior:card:01114:printed-unique
  @card:01114
  Scenario: Card 01114 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 622 |
    When the printed characteristics of card 01114 copy 0 are requested
    Then card 01114 copy 0 exposes these printed characteristics
      | field | value |
      | name | Klaw |
      | type | Villain |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 1 |
      | attribute:HP | 18* |
      | attribute:SCH | 2 |
      | attribute:Stage | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01115:printed-name
  @covers:behavior:card:01115:printed-type
  @covers:behavior:card:01115:printed-traits
  @covers:behavior:card:01115:printed-atk
  @covers:behavior:card:01115:printed-hp
  @covers:behavior:card:01115:printed-sch
  @covers:behavior:card:01115:printed-stage
  @covers:behavior:card:01115:printed-toughness
  @covers:behavior:card:01115:printed-unique
  @card:01115
  Scenario: Card 01115 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw_expert | spider_man | 623 |
    When the printed characteristics of card 01115 copy 0 are requested
    Then card 01115 copy 0 exposes these printed characteristics
      | field | value |
      | name | Klaw |
      | type | Villain |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 2 |
      | attribute:HP | 22* |
      | attribute:SCH | 3 |
      | attribute:Stage | 3 |
      | attribute:Toughness | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01116a:printed-name
  @covers:behavior:card:01116a:printed-type
  @covers:behavior:card:01116a:printed-stage
  @card:01116a
  Scenario: Card 01116a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 624 |
    When the printed characteristics of card 01116a copy 0 are requested
    Then card 01116a copy 0 exposes these printed characteristics
      | field | value |
      | name | Underground Distribution |
      | type | MainScheme |
      | attribute:Stage | 1 |

  @behavior:card:01116b:printed-name
  @covers:behavior:card:01116b:printed-type
  @covers:behavior:card:01116b:printed-escalationthreat
  @covers:behavior:card:01116b:printed-stage
  @covers:behavior:card:01116b:printed-startingthreat
  @covers:behavior:card:01116b:printed-targetthreat
  @card:01116b
  Scenario: Card 01116b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 625 |
    When the printed characteristics of card 01116b copy 0 are requested
    Then card 01116b copy 0 exposes these printed characteristics
      | field | value |
      | name | Underground Distribution |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 1 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 6* |

  @behavior:card:01117a:printed-name
  @covers:behavior:card:01117a:printed-type
  @covers:behavior:card:01117a:printed-stage
  @card:01117a
  Scenario: Card 01117a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 626 |
    When the printed characteristics of card 01117a copy 0 are requested
    Then card 01117a copy 0 exposes these printed characteristics
      | field | value |
      | name | Secret Rendezvous |
      | type | MainScheme |
      | attribute:Stage | 2 |

  @behavior:card:01117b:printed-name
  @covers:behavior:card:01117b:printed-type
  @covers:behavior:card:01117b:printed-escalationthreat
  @covers:behavior:card:01117b:printed-stage
  @covers:behavior:card:01117b:printed-startingthreat
  @covers:behavior:card:01117b:printed-targetthreat
  @card:01117b
  Scenario: Card 01117b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 627 |
    When the printed characteristics of card 01117b copy 0 are requested
    Then card 01117b copy 0 exposes these printed characteristics
      | field | value |
      | name | Secret Rendezvous |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 2 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 8* |

  @behavior:card:01118:printed-name
  @covers:behavior:card:01118:printed-type
  @covers:behavior:card:01118:printed-traits
  @covers:behavior:card:01118:printed-atk
  @covers:behavior:card:01118:printed-boost
  @covers:behavior:card:01118:printed-unique
  @card:01118
  Scenario: Card 01118 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 628 |
    When the printed characteristics of card 01118 copy 0 are requested
    Then card 01118 copy 0 exposes these printed characteristics
      | field | value |
      | name | Sonic Converter |
      | type | Attachment |
      | traits | WEAPON |
      | attribute:ATK+ | 1 |
      | attribute:Boost | 3 |
      | attribute:Unique | 1 |

  @behavior:card:01119:printed-name
  @covers:behavior:card:01119:printed-type
  @covers:behavior:card:01119:printed-traits
  @covers:behavior:card:01119:printed-boost
  @card:01119
  Scenario: Card 01119 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 629 |
    When the printed characteristics of card 01119 copy 0 are requested
    Then card 01119 copy 0 exposes these printed characteristics
      | field | value |
      | name | Solid-Sound Body |
      | type | Attachment |
      | traits | CONDITION |
      | attribute:Boost | 3 |

  @behavior:card:01120:printed-name
  @covers:behavior:card:01120:printed-type
  @covers:behavior:card:01120:printed-traits
  @covers:behavior:card:01120:printed-atk
  @covers:behavior:card:01120:printed-boost
  @covers:behavior:card:01120:printed-guard
  @covers:behavior:card:01120:printed-hp
  @covers:behavior:card:01120:printed-sch
  @covers:behavior:card:01120:printed-toughness
  @card:01120
  Scenario: Card 01120 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 630 |
    When the printed characteristics of card 01120 copy 0 are requested
    Then card 01120 copy 0 exposes these printed characteristics
      | field | value |
      | name | Armored Guard |
      | type | Minion |
      | traits | MERCENARY |
      | attribute:ATK | 1 |
      | attribute:Boost | 1 |
      | attribute:Guard | 1 |
      | attribute:HP | 3 |
      | attribute:SCH | 0 |
      | attribute:Toughness | 1 |

  @behavior:card:01121:printed-name
  @covers:behavior:card:01121:printed-type
  @covers:behavior:card:01121:printed-traits
  @covers:behavior:card:01121:printed-atk
  @covers:behavior:card:01121:printed-hp
  @covers:behavior:card:01121:printed-sch
  @covers:behavior:card:01121:printed-surge
  @card:01121
  Scenario: Card 01121 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 631 |
    When the printed characteristics of card 01121 copy 0 are requested
    Then card 01121 copy 0 exposes these printed characteristics
      | field | value |
      | name | Weapons Runner |
      | type | Minion |
      | traits | MERCENARY |
      | attribute:ATK | 1 |
      | attribute:HP | 2 |
      | attribute:SCH | 1 |
      | attribute:Surge | 1 |

  @behavior:card:01122:printed-name
  @covers:behavior:card:01122:printed-type
  @covers:behavior:card:01122:printed-boost
  @card:01122
  Scenario: Card 01122 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 632 |
    When the printed characteristics of card 01122 copy 0 are requested
    Then card 01122 copy 0 exposes these printed characteristics
      | field | value |
      | name | Klaw's Vengeance |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01123:printed-name
  @covers:behavior:card:01123:printed-type
  @card:01123
  Scenario: Card 01123 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 633 |
    When the printed characteristics of card 01123 copy 0 are requested
    Then card 01123 copy 0 exposes these printed characteristics
      | field | value |
      | name | Sonic Boom |
      | type | Treachery |

  @behavior:card:01124:printed-name
  @covers:behavior:card:01124:printed-type
  @covers:behavior:card:01124:printed-boost
  @card:01124
  Scenario: Card 01124 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 634 |
    When the printed characteristics of card 01124 copy 0 are requested
    Then card 01124 copy 0 exposes these printed characteristics
      | field | value |
      | name | Sound Manipulation |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01125:printed-name
  @covers:behavior:card:01125:printed-type
  @covers:behavior:card:01125:printed-boost
  @covers:behavior:card:01125:printed-crisis
  @covers:behavior:card:01125:printed-startingthreat
  @card:01125
  Scenario: Card 01125 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 635 |
    When the printed characteristics of card 01125 copy 0 are requested
    Then card 01125 copy 0 exposes these printed characteristics
      | field | value |
      | name | Defense Network |
      | type | SideScheme |
      | attribute:Boost | 2 |
      | attribute:Crisis | 1 |
      | attribute:StartingThreat | 2 |

  @behavior:card:01126:printed-name
  @covers:behavior:card:01126:printed-type
  @covers:behavior:card:01126:printed-boost
  @covers:behavior:card:01126:printed-hazard
  @covers:behavior:card:01126:printed-startingthreat
  @card:01126
  Scenario: Card 01126 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 636 |
    When the printed characteristics of card 01126 copy 0 are requested
    Then card 01126 copy 0 exposes these printed characteristics
      | field | value |
      | name | Illegal Arms Factory |
      | type | SideScheme |
      | attribute:Boost | 2 |
      | attribute:Hazard | 1 |
      | attribute:StartingThreat | 3 |

  @behavior:card:01127:printed-name
  @covers:behavior:card:01127:printed-type
  @covers:behavior:card:01127:printed-acceleration
  @covers:behavior:card:01127:printed-startingthreat
  @card:01127
  Scenario: Card 01127 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 637 |
    When the printed characteristics of card 01127 copy 0 are requested
    Then card 01127 copy 0 exposes these printed characteristics
      | field | value |
      | name | The "Immortal" Klaw |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:StartingThreat | 3* |

  @behavior:card:01128:printed-name
  @covers:behavior:card:01128:printed-type
  @covers:behavior:card:01128:printed-acceleration
  @covers:behavior:card:01128:printed-boost
  @covers:behavior:card:01128:printed-startingthreat
  @card:01128
  Scenario: Card 01128 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 638 |
    When the printed characteristics of card 01128 copy 0 are requested
    Then card 01128 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Masters of Evil |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 2 |
      | attribute:StartingThreat | 3* |

  @behavior:card:01129:printed-name
  @covers:behavior:card:01129:printed-type
  @covers:behavior:card:01129:printed-traits
  @covers:behavior:card:01129:printed-atk
  @covers:behavior:card:01129:printed-hp
  @covers:behavior:card:01129:printed-sch
  @covers:behavior:card:01129:printed-unique
  @card:01129
  Scenario: Card 01129 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 639 |
    When the printed characteristics of card 01129 copy 0 are requested
    Then card 01129 copy 0 exposes these printed characteristics
      | field | value |
      | name | Radioactive Man |
      | type | Minion |
      | traits | ELITE/MASTERS OF EVIL |
      | attribute:ATK | 1 |
      | attribute:HP | 7 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01130:printed-name
  @covers:behavior:card:01130:printed-type
  @covers:behavior:card:01130:printed-traits
  @covers:behavior:card:01130:printed-atk
  @covers:behavior:card:01130:printed-hp
  @covers:behavior:card:01130:printed-sch
  @covers:behavior:card:01130:printed-unique
  @card:01130
  Scenario: Card 01130 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 640 |
    When the printed characteristics of card 01130 copy 0 are requested
    Then card 01130 copy 0 exposes these printed characteristics
      | field | value |
      | name | Whirlwind |
      | type | Minion |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 2 |
      | attribute:HP | 6 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01131:printed-name
  @covers:behavior:card:01131:printed-type
  @covers:behavior:card:01131:printed-traits
  @covers:behavior:card:01131:printed-atk
  @covers:behavior:card:01131:printed-hp
  @covers:behavior:card:01131:printed-sch
  @covers:behavior:card:01131:printed-unique
  @card:01131
  Scenario: Card 01131 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 641 |
    When the printed characteristics of card 01131 copy 0 are requested
    Then card 01131 copy 0 exposes these printed characteristics
      | field | value |
      | name | Tiger Shark |
      | type | Minion |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 3 |
      | attribute:HP | 6 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01132:printed-name
  @covers:behavior:card:01132:printed-type
  @covers:behavior:card:01132:printed-traits
  @covers:behavior:card:01132:printed-atk
  @covers:behavior:card:01132:printed-hp
  @covers:behavior:card:01132:printed-sch
  @covers:behavior:card:01132:printed-unique
  @card:01132
  Scenario: Card 01132 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 642 |
    When the printed characteristics of card 01132 copy 0 are requested
    Then card 01132 copy 0 exposes these printed characteristics
      | field | value |
      | name | Melter |
      | type | Minion |
      | traits | MASTERS OF EVIL |
      | attribute:ATK | 3 |
      | attribute:HP | 5 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01133:printed-name
  @covers:behavior:card:01133:printed-type
  @covers:behavior:card:01133:printed-boost
  @card:01133
  Scenario: Card 01133 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | klaw | spider_man | 643 |
    When the printed characteristics of card 01133 copy 0 are requested
    Then card 01133 copy 0 exposes these printed characteristics
      | field | value |
      | name | Masters of Mayhem |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01134:printed-name
  @covers:behavior:card:01134:printed-type
  @covers:behavior:card:01134:printed-traits
  @covers:behavior:card:01134:printed-atk
  @covers:behavior:card:01134:printed-hp
  @covers:behavior:card:01134:printed-sch
  @covers:behavior:card:01134:printed-stage
  @covers:behavior:card:01134:printed-unique
  @card:01134
  Scenario: Card 01134 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 644 |
    When the printed characteristics of card 01134 copy 0 are requested
    Then card 01134 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ultron |
      | type | Villain |
      | traits | ANDROID |
      | attribute:ATK | 2 |
      | attribute:HP | 17* |
      | attribute:SCH | 1 |
      | attribute:Stage | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01135:printed-name
  @covers:behavior:card:01135:printed-type
  @covers:behavior:card:01135:printed-traits
  @covers:behavior:card:01135:printed-atk
  @covers:behavior:card:01135:printed-hp
  @covers:behavior:card:01135:printed-sch
  @covers:behavior:card:01135:printed-stage
  @covers:behavior:card:01135:printed-unique
  @card:01135
  Scenario: Card 01135 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 645 |
    When the printed characteristics of card 01135 copy 0 are requested
    Then card 01135 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ultron |
      | type | Villain |
      | traits | ANDROID |
      | attribute:ATK | 2 |
      | attribute:HP | 22* |
      | attribute:SCH | 2 |
      | attribute:Stage | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01136:printed-name
  @covers:behavior:card:01136:printed-type
  @covers:behavior:card:01136:printed-traits
  @covers:behavior:card:01136:printed-atk
  @covers:behavior:card:01136:printed-hp
  @covers:behavior:card:01136:printed-sch
  @covers:behavior:card:01136:printed-stage
  @covers:behavior:card:01136:printed-unique
  @card:01136
  Scenario: Card 01136 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron_expert | spider_man | 646 |
    When the printed characteristics of card 01136 copy 0 are requested
    Then card 01136 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ultron |
      | type | Villain |
      | traits | ANDROID |
      | attribute:ATK | 4 |
      | attribute:HP | 27* |
      | attribute:SCH | 2 |
      | attribute:Stage | 3 |
      | attribute:Unique | 1 |

  @behavior:card:01137a:printed-name
  @covers:behavior:card:01137a:printed-type
  @covers:behavior:card:01137a:printed-stage
  @card:01137a
  Scenario: Card 01137a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 647 |
    When the printed characteristics of card 01137a copy 0 are requested
    Then card 01137a copy 0 exposes these printed characteristics
      | field | value |
      | name | The Crimson Cowl |
      | type | MainScheme |
      | attribute:Stage | 1 |

  @behavior:card:01137b:printed-name
  @covers:behavior:card:01137b:printed-type
  @covers:behavior:card:01137b:printed-escalationthreat
  @covers:behavior:card:01137b:printed-stage
  @covers:behavior:card:01137b:printed-startingthreat
  @covers:behavior:card:01137b:printed-targetthreat
  @card:01137b
  Scenario: Card 01137b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 648 |
    When the printed characteristics of card 01137b copy 0 are requested
    Then card 01137b copy 0 exposes these printed characteristics
      | field | value |
      | name | The Crimson Cowl |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 1 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 3* |

  @behavior:card:01138a:printed-name
  @covers:behavior:card:01138a:printed-type
  @covers:behavior:card:01138a:printed-stage
  @card:01138a
  Scenario: Card 01138a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 649 |
    When the printed characteristics of card 01138a copy 0 are requested
    Then card 01138a copy 0 exposes these printed characteristics
      | field | value |
      | name | Assault on NORAD |
      | type | MainScheme |
      | attribute:Stage | 2 |

  @behavior:card:01138b:printed-name
  @covers:behavior:card:01138b:printed-type
  @covers:behavior:card:01138b:printed-escalationthreat
  @covers:behavior:card:01138b:printed-stage
  @covers:behavior:card:01138b:printed-startingthreat
  @covers:behavior:card:01138b:printed-targetthreat
  @card:01138b
  Scenario: Card 01138b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 650 |
    When the printed characteristics of card 01138b copy 0 are requested
    Then card 01138b copy 0 exposes these printed characteristics
      | field | value |
      | name | Assault on NORAD |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 2 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 10* |

  @behavior:card:01139a:printed-name
  @covers:behavior:card:01139a:printed-type
  @covers:behavior:card:01139a:printed-stage
  @card:01139a
  Scenario: Card 01139a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 651 |
    When the printed characteristics of card 01139a copy 0 are requested
    Then card 01139a copy 0 exposes these printed characteristics
      | field | value |
      | name | Countdown to Oblivion |
      | type | MainScheme |
      | attribute:Stage | 3 |

  @behavior:card:01139b:printed-name
  @covers:behavior:card:01139b:printed-type
  @covers:behavior:card:01139b:printed-escalationthreat
  @covers:behavior:card:01139b:printed-stage
  @covers:behavior:card:01139b:printed-startingthreat
  @covers:behavior:card:01139b:printed-targetthreat
  @card:01139b
  Scenario: Card 01139b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 652 |
    When the printed characteristics of card 01139b copy 0 are requested
    Then card 01139b copy 0 exposes these printed characteristics
      | field | value |
      | name | Countdown to Oblivion |
      | type | MainScheme |
      | attribute:EscalationThreat | 1* |
      | attribute:Stage | 3 |
      | attribute:StartingThreat | 0 |
      | attribute:TargetThreat | 5* |

  @behavior:card:01140:printed-name
  @covers:behavior:card:01140:printed-type
  @card:01140
  Scenario: Card 01140 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 653 |
    When the printed characteristics of card 01140 copy 0 are requested
    Then card 01140 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ultron Drones |
      | type | Environment |

  @behavior:card:01141:printed-name
  @covers:behavior:card:01141:printed-type
  @covers:behavior:card:01141:printed-traits
  @covers:behavior:card:01141:printed-boost
  @covers:behavior:card:01141:printed-sch
  @card:01141
  Scenario: Card 01141 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 654 |
    When the printed characteristics of card 01141 copy 0 are requested
    Then card 01141 copy 0 exposes these printed characteristics
      | field | value |
      | name | Program Transmitter |
      | type | Attachment |
      | traits | ITEM/TECH |
      | attribute:Boost | 1 |
      | attribute:SCH+ | 1 |

  @behavior:card:01142:printed-name
  @covers:behavior:card:01142:printed-type
  @covers:behavior:card:01142:printed-traits
  @card:01142
  Scenario: Card 01142 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 655 |
    When the printed characteristics of card 01142 copy 0 are requested
    Then card 01142 copy 0 exposes these printed characteristics
      | field | value |
      | name | Upgraded Drones |
      | type | Attachment |
      | traits | CONDITION |

  @behavior:card:01143:printed-name
  @covers:behavior:card:01143:printed-type
  @covers:behavior:card:01143:printed-traits
  @covers:behavior:card:01143:printed-atk
  @covers:behavior:card:01143:printed-boost
  @covers:behavior:card:01143:printed-guard
  @covers:behavior:card:01143:printed-hp
  @covers:behavior:card:01143:printed-sch
  @card:01143
  Scenario: Card 01143 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 656 |
    When the printed characteristics of card 01143 copy 0 are requested
    Then card 01143 copy 0 exposes these printed characteristics
      | field | value |
      | name | Advanced Ultron Drone |
      | type | Minion |
      | traits | DRONE |
      | attribute:ATK | 1 |
      | attribute:Boost | 2 |
      | attribute:Guard | 1 |
      | attribute:HP | 4 |
      | attribute:SCH | 1 |

  @behavior:card:01144a:printed-name
  @covers:behavior:card:01144a:printed-type
  @card:01144a
  Scenario: Card 01144a exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 657 |
    When the printed characteristics of card 01144a copy 0 are requested
    Then card 01144a copy 0 exposes these printed characteristics
      | field | value |
      | name | Android Efficiency |
      | type | Treachery |

  @behavior:card:01144b:printed-name
  @covers:behavior:card:01144b:printed-type
  @card:01144b
  Scenario: Card 01144b exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 658 |
    When the printed characteristics of card 01144b copy 0 are requested
    Then card 01144b copy 0 exposes these printed characteristics
      | field | value |
      | name | Android Efficiency |
      | type | Treachery |

  @behavior:card:01144c:printed-name
  @covers:behavior:card:01144c:printed-type
  @card:01144c
  Scenario: Card 01144c exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 659 |
    When the printed characteristics of card 01144c copy 0 are requested
    Then card 01144c copy 0 exposes these printed characteristics
      | field | value |
      | name | Android Efficiency |
      | type | Treachery |

  @behavior:card:01145:printed-name
  @covers:behavior:card:01145:printed-type
  @covers:behavior:card:01145:printed-boost
  @card:01145
  Scenario: Card 01145 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 660 |
    When the printed characteristics of card 01145 copy 0 are requested
    Then card 01145 copy 0 exposes these printed characteristics
      | field | value |
      | name | Rage of Ultron |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01146:printed-name
  @covers:behavior:card:01146:printed-type
  @covers:behavior:card:01146:printed-boost
  @card:01146
  Scenario: Card 01146 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 661 |
    When the printed characteristics of card 01146 copy 0 are requested
    Then card 01146 copy 0 exposes these printed characteristics
      | field | value |
      | name | Repair Sequence |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01147:printed-name
  @covers:behavior:card:01147:printed-type
  @covers:behavior:card:01147:printed-boost
  @card:01147
  Scenario: Card 01147 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 662 |
    When the printed characteristics of card 01147 copy 0 are requested
    Then card 01147 copy 0 exposes these printed characteristics
      | field | value |
      | name | Swarm Attack |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01148:printed-name
  @covers:behavior:card:01148:printed-type
  @covers:behavior:card:01148:printed-acceleration
  @covers:behavior:card:01148:printed-boost
  @covers:behavior:card:01148:printed-startingthreat
  @card:01148
  Scenario: Card 01148 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 663 |
    When the printed characteristics of card 01148 copy 0 are requested
    Then card 01148 copy 0 exposes these printed characteristics
      | field | value |
      | name | Drone Factory |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 2 |
      | attribute:StartingThreat | 4 |

  @behavior:card:01149:printed-name
  @covers:behavior:card:01149:printed-type
  @covers:behavior:card:01149:printed-boost
  @covers:behavior:card:01149:printed-hazard
  @covers:behavior:card:01149:printed-startingthreat
  @card:01149
  Scenario: Card 01149 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 664 |
    When the printed characteristics of card 01149 copy 0 are requested
    Then card 01149 copy 0 exposes these printed characteristics
      | field | value |
      | name | Invasive AI |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Hazard | 1 |
      | attribute:StartingThreat | 3* |

  @behavior:card:01150:printed-name
  @covers:behavior:card:01150:printed-type
  @covers:behavior:card:01150:printed-boost
  @covers:behavior:card:01150:printed-hazard
  @covers:behavior:card:01150:printed-startingthreat
  @card:01150
  Scenario: Card 01150 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 665 |
    When the printed characteristics of card 01150 copy 0 are requested
    Then card 01150 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ultron's Imperative |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Hazard | 1 |
      | attribute:StartingThreat | 2* |

  @behavior:card:01151:printed-name
  @covers:behavior:card:01151:printed-type
  @covers:behavior:card:01151:printed-boost
  @covers:behavior:card:01151:printed-crisis
  @covers:behavior:card:01151:printed-startingthreat
  @card:01151
  Scenario: Card 01151 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 666 |
    When the printed characteristics of card 01151 copy 0 are requested
    Then card 01151 copy 0 exposes these printed characteristics
      | field | value |
      | name | Under Attack |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Crisis | 1 |
      | attribute:StartingThreat | 3 |

  @behavior:card:01152:printed-name
  @covers:behavior:card:01152:printed-type
  @covers:behavior:card:01152:printed-traits
  @covers:behavior:card:01152:printed-boost
  @card:01152
  Scenario: Card 01152 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 667 |
    When the printed characteristics of card 01152 copy 0 are requested
    Then card 01152 copy 0 exposes these printed characteristics
      | field | value |
      | name | Vibranium Armor |
      | type | Attachment |
      | traits | ARMOR |
      | attribute:Boost | 1 |

  @behavior:card:01153:printed-name
  @covers:behavior:card:01153:printed-type
  @covers:behavior:card:01153:printed-traits
  @covers:behavior:card:01153:printed-atk
  @covers:behavior:card:01153:printed-boost
  @card:01153
  Scenario: Card 01153 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 668 |
    When the printed characteristics of card 01153 copy 0 are requested
    Then card 01153 copy 0 exposes these printed characteristics
      | field | value |
      | name | Concussion Blasters |
      | type | Attachment |
      | traits | WEAPON |
      | attribute:ATK+ | 1 |
      | attribute:Boost | 1 |

  @behavior:card:01154:printed-name
  @covers:behavior:card:01154:printed-type
  @card:01154
  Scenario: Card 01154 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | ultron | spider_man | 669 |
    When the printed characteristics of card 01154 copy 0 are requested
    Then card 01154 copy 0 exposes these printed characteristics
      | field | value |
      | name | Concussive Blast |
      | type | Treachery |

  @behavior:card:01155:printed-name
  @covers:behavior:card:01155:printed-type
  @covers:behavior:card:01155:printed-boost
  @covers:behavior:card:01155:printed-giveto
  @card:01155
  Scenario: Card 01155 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 670 |
    When the printed characteristics of card 01155 copy 0 are requested
    Then card 01155 copy 0 exposes these printed characteristics
      | field | value |
      | name | Affairs of State |
      | type | Obligation |
      | attribute:Boost | 2 |
      | attribute:GiveTo | T'Challa |

  @behavior:card:01156:printed-name
  @covers:behavior:card:01156:printed-type
  @covers:behavior:card:01156:printed-boost
  @covers:behavior:card:01156:printed-hazard
  @covers:behavior:card:01156:printed-nemesis
  @covers:behavior:card:01156:printed-startingthreat
  @card:01156
  Scenario: Card 01156 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 671 |
    When the printed characteristics of card 01156 copy 0 are requested
    Then card 01156 copy 0 exposes these printed characteristics
      | field | value |
      | name | Usurp The Throne |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Hazard | 1 |
      | attribute:Nemesis | T'Challa |
      | attribute:StartingThreat | 3* |

  @behavior:card:01157:printed-name
  @covers:behavior:card:01157:printed-type
  @covers:behavior:card:01157:printed-traits
  @covers:behavior:card:01157:printed-atk
  @covers:behavior:card:01157:printed-boost
  @covers:behavior:card:01157:printed-hp
  @covers:behavior:card:01157:printed-nemesis
  @covers:behavior:card:01157:printed-sch
  @covers:behavior:card:01157:printed-unique
  @card:01157
  Scenario: Card 01157 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 672 |
    When the printed characteristics of card 01157 copy 0 are requested
    Then card 01157 copy 0 exposes these printed characteristics
      | field | value |
      | name | Killmonger |
      | type | Minion |
      | traits | ASSASSIN/ELITE/MERCENARY |
      | attribute:ATK | 2 |
      | attribute:Boost | 2 |
      | attribute:HP | 5 |
      | attribute:Nemesis | T'Challa |
      | attribute:SCH | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01158:printed-name
  @covers:behavior:card:01158:printed-type
  @covers:behavior:card:01158:printed-boost
  @covers:behavior:card:01158:printed-nemesis
  @covers:behavior:card:01158:printed-surge
  @card:01158
  Scenario: Card 01158 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 673 |
    When the printed characteristics of card 01158 copy 0 are requested
    Then card 01158 copy 0 exposes these printed characteristics
      | field | value |
      | name | Heart-Shaped Herb |
      | type | Treachery |
      | attribute:Boost | 1 |
      | attribute:Nemesis | T'Challa |
      | attribute:Surge | 1 |

  @behavior:card:01159:printed-name
  @covers:behavior:card:01159:printed-type
  @covers:behavior:card:01159:printed-boost
  @covers:behavior:card:01159:printed-nemesis
  @card:01159
  Scenario: Card 01159 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | black_panther | 674 |
    When the printed characteristics of card 01159 copy 0 are requested
    Then card 01159 copy 0 exposes these printed characteristics
      | field | value |
      | name | Ritual Combat |
      | type | Treachery |
      | attribute:Boost | 2 |
      | attribute:Nemesis | T'Challa |

  @behavior:card:01160:printed-name
  @covers:behavior:card:01160:printed-type
  @covers:behavior:card:01160:printed-boost
  @covers:behavior:card:01160:printed-giveto
  @card:01160
  Scenario: Card 01160 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 675 |
    When the printed characteristics of card 01160 copy 0 are requested
    Then card 01160 copy 0 exposes these printed characteristics
      | field | value |
      | name | Legal Work |
      | type | Obligation |
      | attribute:Boost | 2 |
      | attribute:GiveTo | Jennifer Walters |

  @behavior:card:01161:printed-name
  @covers:behavior:card:01161:printed-type
  @covers:behavior:card:01161:printed-boost
  @covers:behavior:card:01161:printed-crisis
  @covers:behavior:card:01161:printed-nemesis
  @covers:behavior:card:01161:printed-startingthreat
  @card:01161
  Scenario: Card 01161 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 676 |
    When the printed characteristics of card 01161 copy 0 are requested
    Then card 01161 copy 0 exposes these printed characteristics
      | field | value |
      | name | Personal Challenge |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Crisis | 1 |
      | attribute:Nemesis | Jennifer Walters |
      | attribute:StartingThreat | 3 |

  @behavior:card:01162:printed-name
  @covers:behavior:card:01162:printed-type
  @covers:behavior:card:01162:printed-traits
  @covers:behavior:card:01162:printed-atk
  @covers:behavior:card:01162:printed-boost
  @covers:behavior:card:01162:printed-hp
  @covers:behavior:card:01162:printed-nemesis
  @covers:behavior:card:01162:printed-sch
  @covers:behavior:card:01162:printed-unique
  @card:01162
  Scenario: Card 01162 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 677 |
    When the printed characteristics of card 01162 copy 0 are requested
    Then card 01162 copy 0 exposes these printed characteristics
      | field | value |
      | name | Titania |
      | type | Minion |
      | traits | BRUTE/ELITE |
      | attribute:ATK | 0 |
      | attribute:Boost | 2 |
      | attribute:HP | 6 |
      | attribute:Nemesis | Jennifer Walters |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01163:printed-name
  @covers:behavior:card:01163:printed-type
  @covers:behavior:card:01163:printed-traits
  @covers:behavior:card:01163:printed-boost
  @covers:behavior:card:01163:printed-nemesis
  @card:01163
  Scenario: Card 01163 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 678 |
    When the printed characteristics of card 01163 copy 0 are requested
    Then card 01163 copy 0 exposes these printed characteristics
      | field | value |
      | name | Genetically Enhanced |
      | type | Attachment |
      | traits | CONDITION |
      | attribute:Boost | 1 |
      | attribute:Nemesis | Jennifer Walters |

  @behavior:card:01164:printed-name
  @covers:behavior:card:01164:printed-type
  @covers:behavior:card:01164:printed-boost
  @covers:behavior:card:01164:printed-nemesis
  @card:01164
  Scenario: Card 01164 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | she_hulk | 679 |
    When the printed characteristics of card 01164 copy 0 are requested
    Then card 01164 copy 0 exposes these printed characteristics
      | field | value |
      | name | Titania's Fury |
      | type | Treachery |
      | attribute:Boost | 1 |
      | attribute:Nemesis | Jennifer Walters |

  @behavior:card:01165:printed-name
  @covers:behavior:card:01165:printed-type
  @covers:behavior:card:01165:printed-boost
  @covers:behavior:card:01165:printed-giveto
  @card:01165
  Scenario: Card 01165 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 680 |
    When the printed characteristics of card 01165 copy 0 are requested
    Then card 01165 copy 0 exposes these printed characteristics
      | field | value |
      | name | Eviction Notice |
      | type | Obligation |
      | attribute:Boost | 2 |
      | attribute:GiveTo | Peter Parker |

  @behavior:card:01166:printed-name
  @covers:behavior:card:01166:printed-type
  @covers:behavior:card:01166:printed-acceleration
  @covers:behavior:card:01166:printed-boost
  @covers:behavior:card:01166:printed-nemesis
  @covers:behavior:card:01166:printed-startingthreat
  @card:01166
  Scenario: Card 01166 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 681 |
    When the printed characteristics of card 01166 copy 0 are requested
    Then card 01166 copy 0 exposes these printed characteristics
      | field | value |
      | name | Highway Robbery |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 3 |
      | attribute:Nemesis | Peter Parker |
      | attribute:StartingThreat | 3* |

  @behavior:card:01167:printed-name
  @covers:behavior:card:01167:printed-type
  @covers:behavior:card:01167:printed-traits
  @covers:behavior:card:01167:printed-atk
  @covers:behavior:card:01167:printed-boost
  @covers:behavior:card:01167:printed-hp
  @covers:behavior:card:01167:printed-nemesis
  @covers:behavior:card:01167:printed-quickstrike
  @covers:behavior:card:01167:printed-sch
  @covers:behavior:card:01167:printed-unique
  @card:01167
  Scenario: Card 01167 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 682 |
    When the printed characteristics of card 01167 copy 0 are requested
    Then card 01167 copy 0 exposes these printed characteristics
      | field | value |
      | name | Vulture |
      | type | Minion |
      | traits | CRIMINAL |
      | attribute:ATK | 3 |
      | attribute:Boost | 2 |
      | attribute:HP | 4 |
      | attribute:Nemesis | Peter Parker |
      | attribute:Quickstrike | 1 |
      | attribute:SCH | 1 |
      | attribute:Unique | 1 |

  @behavior:card:01168:printed-name
  @covers:behavior:card:01168:printed-type
  @covers:behavior:card:01168:printed-nemesis
  @card:01168
  Scenario: Card 01168 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 683 |
    When the printed characteristics of card 01168 copy 0 are requested
    Then card 01168 copy 0 exposes these printed characteristics
      | field | value |
      | name | Sweeping Swoop |
      | type | Treachery |
      | attribute:Nemesis | Peter Parker |

  @behavior:card:01169:printed-name
  @covers:behavior:card:01169:printed-type
  @covers:behavior:card:01169:printed-boost
  @covers:behavior:card:01169:printed-nemesis
  @card:01169
  Scenario: Card 01169 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 684 |
    When the printed characteristics of card 01169 copy 0 are requested
    Then card 01169 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Vulture's Plans |
      | type | Treachery |
      | attribute:Boost | 2 |
      | attribute:Nemesis | Peter Parker |

  @behavior:card:01170:printed-name
  @covers:behavior:card:01170:printed-type
  @covers:behavior:card:01170:printed-boost
  @covers:behavior:card:01170:printed-giveto
  @card:01170
  Scenario: Card 01170 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 685 |
    When the printed characteristics of card 01170 copy 0 are requested
    Then card 01170 copy 0 exposes these printed characteristics
      | field | value |
      | name | Business Problems |
      | type | Obligation |
      | attribute:Boost | 2 |
      | attribute:GiveTo | Tony Stark |

  @behavior:card:01171:printed-name
  @covers:behavior:card:01171:printed-type
  @covers:behavior:card:01171:printed-acceleration
  @covers:behavior:card:01171:printed-boost
  @covers:behavior:card:01171:printed-nemesis
  @covers:behavior:card:01171:printed-startingthreat
  @card:01171
  Scenario: Card 01171 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 686 |
    When the printed characteristics of card 01171 copy 0 are requested
    Then card 01171 copy 0 exposes these printed characteristics
      | field | value |
      | name | Imminent Overload |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 3 |
      | attribute:Nemesis | Tony Stark |
      | attribute:StartingThreat | 3 |

  @behavior:card:01172:printed-name
  @covers:behavior:card:01172:printed-type
  @covers:behavior:card:01172:printed-traits
  @covers:behavior:card:01172:printed-atk
  @covers:behavior:card:01172:printed-boost
  @covers:behavior:card:01172:printed-hp
  @covers:behavior:card:01172:printed-nemesis
  @covers:behavior:card:01172:printed-retaliate
  @covers:behavior:card:01172:printed-sch
  @covers:behavior:card:01172:printed-unique
  @card:01172
  Scenario: Card 01172 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 687 |
    When the printed characteristics of card 01172 copy 0 are requested
    Then card 01172 copy 0 exposes these printed characteristics
      | field | value |
      | name | Whiplash |
      | type | Minion |
      | traits | CRIMINAL |
      | attribute:ATK | 3 |
      | attribute:Boost | 2 |
      | attribute:HP | 4 |
      | attribute:Nemesis | Tony Stark |
      | attribute:Retaliate | 1 |
      | attribute:SCH | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01173:printed-name
  @covers:behavior:card:01173:printed-type
  @covers:behavior:card:01173:printed-nemesis
  @card:01173
  Scenario: Card 01173 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 688 |
    When the printed characteristics of card 01173 copy 0 are requested
    Then card 01173 copy 0 exposes these printed characteristics
      | field | value |
      | name | Electric Whip Attack |
      | type | Treachery |
      | attribute:Nemesis | Tony Stark |

  @behavior:card:01174:printed-name
  @covers:behavior:card:01174:printed-type
  @covers:behavior:card:01174:printed-boost
  @covers:behavior:card:01174:printed-nemesis
  @card:01174
  Scenario: Card 01174 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | iron_man | 689 |
    When the printed characteristics of card 01174 copy 0 are requested
    Then card 01174 copy 0 exposes these printed characteristics
      | field | value |
      | name | Electromagnetic Backlash |
      | type | Treachery |
      | attribute:Boost | 2 |
      | attribute:Nemesis | Tony Stark |

  @behavior:card:01175:printed-name
  @covers:behavior:card:01175:printed-type
  @covers:behavior:card:01175:printed-boost
  @covers:behavior:card:01175:printed-giveto
  @card:01175
  Scenario: Card 01175 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 690 |
    When the printed characteristics of card 01175 copy 0 are requested
    Then card 01175 copy 0 exposes these printed characteristics
      | field | value |
      | name | Family Emergency |
      | type | Obligation |
      | attribute:Boost | 2 |
      | attribute:GiveTo | Carol Danvers |

  @behavior:card:01176:printed-name
  @covers:behavior:card:01176:printed-type
  @covers:behavior:card:01176:printed-boost
  @covers:behavior:card:01176:printed-hazard
  @covers:behavior:card:01176:printed-nemesis
  @covers:behavior:card:01176:printed-startingthreat
  @card:01176
  Scenario: Card 01176 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 691 |
    When the printed characteristics of card 01176 copy 0 are requested
    Then card 01176 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Psyche-Magnitron |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Hazard | 1 |
      | attribute:Nemesis | Carol Danvers |
      | attribute:StartingThreat | 3 |

  @behavior:card:01177:printed-name
  @covers:behavior:card:01177:printed-type
  @covers:behavior:card:01177:printed-traits
  @covers:behavior:card:01177:printed-atk
  @covers:behavior:card:01177:printed-boost
  @covers:behavior:card:01177:printed-hp
  @covers:behavior:card:01177:printed-nemesis
  @covers:behavior:card:01177:printed-sch
  @covers:behavior:card:01177:printed-unique
  @card:01177
  Scenario: Card 01177 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 692 |
    When the printed characteristics of card 01177 copy 0 are requested
    Then card 01177 copy 0 exposes these printed characteristics
      | field | value |
      | name | Yon-Rogg |
      | type | Minion |
      | traits | ELITE/KREE |
      | attribute:ATK | 3 |
      | attribute:Boost | 2 |
      | attribute:HP | 5 |
      | attribute:Nemesis | Carol Danvers |
      | attribute:SCH | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01178:printed-name
  @covers:behavior:card:01178:printed-type
  @covers:behavior:card:01178:printed-nemesis
  @covers:behavior:card:01178:printed-surge
  @card:01178
  Scenario: Card 01178 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 693 |
    When the printed characteristics of card 01178 copy 0 are requested
    Then card 01178 copy 0 exposes these printed characteristics
      | field | value |
      | name | Kree Manipulator |
      | type | Treachery |
      | attribute:Nemesis | Carol Danvers |
      | attribute:Surge | 1 |

  @behavior:card:01179:printed-name
  @covers:behavior:card:01179:printed-type
  @covers:behavior:card:01179:printed-boost
  @covers:behavior:card:01179:printed-nemesis
  @card:01179
  Scenario: Card 01179 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | captain_marvel | 694 |
    When the printed characteristics of card 01179 copy 0 are requested
    Then card 01179 copy 0 exposes these printed characteristics
      | field | value |
      | name | Yon-Rogg's Treason |
      | type | Treachery |
      | attribute:Boost | 1 |
      | attribute:Nemesis | Carol Danvers |

  @behavior:card:01180:printed-name
  @covers:behavior:card:01180:printed-type
  @covers:behavior:card:01180:printed-boost
  @covers:behavior:card:01180:printed-hazard
  @covers:behavior:card:01180:printed-startingthreat
  @card:01180
  Scenario: Card 01180 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | legions_of_hydra | 695 |
    When the printed characteristics of card 01180 copy 0 are requested
    Then card 01180 copy 0 exposes these printed characteristics
      | field | value |
      | name | Legions of Hydra |
      | type | SideScheme |
      | attribute:Boost | 3 |
      | attribute:Hazard | 1 |
      | attribute:StartingThreat | 3 |

  @behavior:card:01181:printed-name
  @covers:behavior:card:01181:printed-type
  @covers:behavior:card:01181:printed-traits
  @covers:behavior:card:01181:printed-atk
  @covers:behavior:card:01181:printed-boost
  @covers:behavior:card:01181:printed-hp
  @covers:behavior:card:01181:printed-sch
  @covers:behavior:card:01181:printed-unique
  @card:01181
  Scenario: Card 01181 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | legions_of_hydra | 696 |
    When the printed characteristics of card 01181 copy 0 are requested
    Then card 01181 copy 0 exposes these printed characteristics
      | field | value |
      | name | Madame Hydra |
      | type | Minion |
      | traits | ELITE/HYDRA |
      | attribute:ATK | 2 |
      | attribute:Boost | 2 |
      | attribute:HP | 6 |
      | attribute:SCH | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01182:printed-name
  @covers:behavior:card:01182:printed-type
  @covers:behavior:card:01182:printed-traits
  @covers:behavior:card:01182:printed-atk
  @covers:behavior:card:01182:printed-boost
  @covers:behavior:card:01182:printed-guard
  @covers:behavior:card:01182:printed-hp
  @covers:behavior:card:01182:printed-sch
  @card:01182
  Scenario: Card 01182 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | legions_of_hydra | 697 |
    When the printed characteristics of card 01182 copy 0 are requested
    Then card 01182 copy 0 exposes these printed characteristics
      | field | value |
      | name | Hydra Soldier |
      | type | Minion |
      | traits | HYDRA |
      | attribute:ATK | 2 |
      | attribute:Boost | 1 |
      | attribute:Guard | 1 |
      | attribute:HP | 4 |
      | attribute:SCH | 1 |

  @behavior:card:01183:printed-name
  @covers:behavior:card:01183:printed-type
  @covers:behavior:card:01183:printed-acceleration
  @covers:behavior:card:01183:printed-boost
  @covers:behavior:card:01183:printed-startingthreat
  @card:01183
  Scenario: Card 01183 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | the_doomsday_chair | 698 |
    When the printed characteristics of card 01183 copy 0 are requested
    Then card 01183 copy 0 exposes these printed characteristics
      | field | value |
      | name | The Doomsday Chair |
      | type | SideScheme |
      | attribute:Acceleration | 1 |
      | attribute:Boost | 3 |
      | attribute:StartingThreat | 8 |

  @behavior:card:01184:printed-name
  @covers:behavior:card:01184:printed-type
  @covers:behavior:card:01184:printed-traits
  @covers:behavior:card:01184:printed-atk
  @covers:behavior:card:01184:printed-boost
  @covers:behavior:card:01184:printed-hp
  @covers:behavior:card:01184:printed-retaliate
  @covers:behavior:card:01184:printed-sch
  @covers:behavior:card:01184:printed-unique
  @card:01184
  Scenario: Card 01184 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | the_doomsday_chair | 699 |
    When the printed characteristics of card 01184 copy 0 are requested
    Then card 01184 copy 0 exposes these printed characteristics
      | field | value |
      | name | M.O.D.O.K. |
      | type | Minion |
      | traits | CYBORG/ELITE |
      | attribute:ATK | 2 |
      | attribute:Boost | 2 |
      | attribute:HP | 8 |
      | attribute:Retaliate | 2 |
      | attribute:SCH | 2 |
      | attribute:Unique | 1 |

  @behavior:card:01185:printed-name
  @covers:behavior:card:01185:printed-type
  @covers:behavior:card:01185:printed-traits
  @covers:behavior:card:01185:printed-boost
  @covers:behavior:card:01185:printed-surge
  @card:01185
  Scenario: Card 01185 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | modular sets | seed |
      | rhino | spider_man | the_doomsday_chair | 700 |
    When the printed characteristics of card 01185 copy 0 are requested
    Then card 01185 copy 0 exposes these printed characteristics
      | field | value |
      | name | Biomechanical Upgrades |
      | type | Attachment |
      | traits | TECH |
      | attribute:Boost | 1 |
      | attribute:Surge | 1 |

  @behavior:card:01186:printed-name
  @covers:behavior:card:01186:printed-type
  @card:01186
  Scenario: Card 01186 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 701 |
    When the printed characteristics of card 01186 copy 0 are requested
    Then card 01186 copy 0 exposes these printed characteristics
      | field | value |
      | name | Advance |
      | type | Treachery |

  @behavior:card:01187:printed-name
  @covers:behavior:card:01187:printed-type
  @card:01187
  Scenario: Card 01187 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 702 |
    When the printed characteristics of card 01187 copy 0 are requested
    Then card 01187 copy 0 exposes these printed characteristics
      | field | value |
      | name | Assault |
      | type | Treachery |

  @behavior:card:01188:printed-name
  @covers:behavior:card:01188:printed-type
  @covers:behavior:card:01188:printed-boost
  @card:01188
  Scenario: Card 01188 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 703 |
    When the printed characteristics of card 01188 copy 0 are requested
    Then card 01188 copy 0 exposes these printed characteristics
      | field | value |
      | name | Caught Off Guard |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01189:printed-name
  @covers:behavior:card:01189:printed-type
  @covers:behavior:card:01189:printed-boost
  @card:01189
  Scenario: Card 01189 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 704 |
    When the printed characteristics of card 01189 copy 0 are requested
    Then card 01189 copy 0 exposes these printed characteristics
      | field | value |
      | name | Gang-Up |
      | type | Treachery |
      | attribute:Boost | 1 |

  @behavior:card:01190:printed-name
  @covers:behavior:card:01190:printed-type
  @covers:behavior:card:01190:printed-boost
  @card:01190
  Scenario: Card 01190 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino | spider_man | 705 |
    When the printed characteristics of card 01190 copy 0 are requested
    Then card 01190 copy 0 exposes these printed characteristics
      | field | value |
      | name | Shadow of the Past |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01191:printed-name
  @covers:behavior:card:01191:printed-type
  @covers:behavior:card:01191:printed-boost
  @covers:behavior:card:01191:printed-surge
  @card:01191
  Scenario: Card 01191 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino_expert | spider_man | 706 |
    When the printed characteristics of card 01191 copy 0 are requested
    Then card 01191 copy 0 exposes these printed characteristics
      | field | value |
      | name | Exhaustion |
      | type | Treachery |
      | attribute:Boost | 2 |
      | attribute:Surge | 1 |

  @behavior:card:01192:printed-name
  @covers:behavior:card:01192:printed-type
  @covers:behavior:card:01192:printed-boost
  @card:01192
  Scenario: Card 01192 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino_expert | spider_man | 707 |
    When the printed characteristics of card 01192 copy 0 are requested
    Then card 01192 copy 0 exposes these printed characteristics
      | field | value |
      | name | Masterplan |
      | type | Treachery |
      | attribute:Boost | 2 |

  @behavior:card:01193:printed-name
  @covers:behavior:card:01193:printed-type
  @covers:behavior:card:01193:printed-boost
  @covers:behavior:card:01193:printed-surge
  @card:01193
  Scenario: Card 01193 exposes its printed face
    # The table is generated from this face's canonical cards.json record;
    # the engine must expose each fact without reinterpretation.
    Given a canonical Core scene is dealt
      | campaign | heroes | seed |
      | rhino_expert | spider_man | 708 |
    When the printed characteristics of card 01193 copy 0 are requested
    Then card 01193 copy 0 exposes these printed characteristics
      | field | value |
      | name | Under Fire |
      | type | Treachery |
      | attribute:Boost | 3 |
      | attribute:Surge | 1 |
