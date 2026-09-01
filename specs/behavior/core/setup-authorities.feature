@core
Feature: Core setup authorities
  Each authored Core setup record deals the complete physical product it names.

  @behavior:setup:campaign:rhino:setup-villain
  @covers:behavior:setup:campaign:rhino:setup-encounter-sets
  @covers:behavior:setup:campaign:rhino:setup-encounters
  @covers:behavior:setup:campaign:rhino:setup-expert
  @covers:behavior:setup:campaign:rhino:setup-modular-sets
  @covers:behavior:setup:campaign:rhino:setup-schemes
  @setup:campaign:rhino
  Scenario: The Rhino setup record deals its complete standard game
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1091 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:rhino"

  @behavior:setup:campaign:rhino_expert:setup-villain
  @covers:behavior:setup:campaign:rhino_expert:setup-encounter-sets
  @covers:behavior:setup:campaign:rhino_expert:setup-encounters
  @covers:behavior:setup:campaign:rhino_expert:setup-expert
  @covers:behavior:setup:campaign:rhino_expert:setup-modular-sets
  @covers:behavior:setup:campaign:rhino_expert:setup-schemes
  @setup:campaign:rhino_expert
  Scenario: The expert Rhino setup record deals its complete game
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 1092 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:rhino_expert"

  @behavior:setup:campaign:klaw:setup-villain
  @covers:behavior:setup:campaign:klaw:setup-encounter-sets
  @covers:behavior:setup:campaign:klaw:setup-encounters
  @covers:behavior:setup:campaign:klaw:setup-expert
  @covers:behavior:setup:campaign:klaw:setup-modular-sets
  @covers:behavior:setup:campaign:klaw:setup-schemes
  @setup:campaign:klaw
  Scenario: The Klaw setup record deals its complete standard game
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 1093 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:klaw"

  @behavior:setup:campaign:klaw_expert:setup-villain
  @covers:behavior:setup:campaign:klaw_expert:setup-encounter-sets
  @covers:behavior:setup:campaign:klaw_expert:setup-encounters
  @covers:behavior:setup:campaign:klaw_expert:setup-expert
  @covers:behavior:setup:campaign:klaw_expert:setup-modular-sets
  @covers:behavior:setup:campaign:klaw_expert:setup-schemes
  @setup:campaign:klaw_expert
  Scenario: The expert Klaw setup record deals its complete game
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 1094 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:klaw_expert"

  @behavior:setup:campaign:ultron:setup-villain
  @covers:behavior:setup:campaign:ultron:setup-encounter-sets
  @covers:behavior:setup:campaign:ultron:setup-encounters
  @covers:behavior:setup:campaign:ultron:setup-expert
  @covers:behavior:setup:campaign:ultron:setup-modular-sets
  @covers:behavior:setup:campaign:ultron:setup-schemes
  @covers:behavior:setup:campaign:ultron:setup-set-aside
  @setup:campaign:ultron
  Scenario: The Ultron setup record deals its complete standard game
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1095 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:ultron"

  @behavior:setup:campaign:ultron_expert:setup-villain
  @covers:behavior:setup:campaign:ultron_expert:setup-encounter-sets
  @covers:behavior:setup:campaign:ultron_expert:setup-encounters
  @covers:behavior:setup:campaign:ultron_expert:setup-expert
  @covers:behavior:setup:campaign:ultron_expert:setup-modular-sets
  @covers:behavior:setup:campaign:ultron_expert:setup-schemes
  @covers:behavior:setup:campaign:ultron_expert:setup-set-aside
  @setup:campaign:ultron_expert
  Scenario: The expert Ultron setup record deals its complete game
    Given a canonical Core scene is dealt
      | campaign      | heroes     | seed |
      | ultron_expert | spider_man | 1096 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:campaign:ultron_expert"

  @behavior:setup:hero:spider_man:setup-hero
  @covers:behavior:setup:hero:spider_man:setup-hero-deck
  @covers:behavior:setup:hero:spider_man:setup-nemesis-set
  @covers:behavior:setup:hero:spider_man:setup-obligations
  @covers:behavior:setup:hero:spider_man:setup-player-deck
  @setup:hero:spider_man
  Scenario: The Spider-Man setup record deals every owned and set-aside card
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1097 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:hero:spider_man"

  @behavior:setup:hero:captain_marvel:setup-hero
  @covers:behavior:setup:hero:captain_marvel:setup-hero-deck
  @covers:behavior:setup:hero:captain_marvel:setup-nemesis-set
  @covers:behavior:setup:hero:captain_marvel:setup-obligations
  @covers:behavior:setup:hero:captain_marvel:setup-player-deck
  @setup:hero:captain_marvel
  Scenario: The Captain Marvel setup record deals every owned and set-aside card
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1098 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:hero:captain_marvel"

  @behavior:setup:hero:she_hulk:setup-hero
  @covers:behavior:setup:hero:she_hulk:setup-hero-deck
  @covers:behavior:setup:hero:she_hulk:setup-nemesis-set
  @covers:behavior:setup:hero:she_hulk:setup-obligations
  @covers:behavior:setup:hero:she_hulk:setup-player-deck
  @setup:hero:she_hulk
  Scenario: The She-Hulk setup record deals every owned and set-aside card
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 1099 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:hero:she_hulk"

  @behavior:setup:hero:iron_man:setup-hero
  @covers:behavior:setup:hero:iron_man:setup-hero-deck
  @covers:behavior:setup:hero:iron_man:setup-nemesis-set
  @covers:behavior:setup:hero:iron_man:setup-obligations
  @covers:behavior:setup:hero:iron_man:setup-player-deck
  @setup:hero:iron_man
  Scenario: The Iron Man setup record deals every owned and set-aside card
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1100 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:hero:iron_man"

  @behavior:setup:hero:black_panther:setup-hero
  @covers:behavior:setup:hero:black_panther:setup-hero-deck
  @covers:behavior:setup:hero:black_panther:setup-nemesis-set
  @covers:behavior:setup:hero:black_panther:setup-obligations
  @covers:behavior:setup:hero:black_panther:setup-player-deck
  @setup:hero:black_panther
  Scenario: The Black Panther setup record deals every owned and set-aside card
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 1101 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:hero:black_panther"

  @behavior:setup:encounter-set:standard:setup-encounters
  @setup:encounter-set:standard
  Scenario: The Standard setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1102 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:standard"

  @behavior:setup:encounter-set:expert:setup-encounters
  @setup:encounter-set:expert
  Scenario: The Expert setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 1103 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:expert"

  @behavior:setup:encounter-set:bomb_scare:setup-encounters
  @setup:encounter-set:bomb_scare
  Scenario: The Bomb Scare setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1104 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:bomb_scare"

  @behavior:setup:encounter-set:masters_of_evil:setup-encounters
  @setup:encounter-set:masters_of_evil
  Scenario: The Masters of Evil setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 1105 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:masters_of_evil"

  @behavior:setup:encounter-set:under_attack:setup-encounters
  @setup:encounter-set:under_attack
  Scenario: The Under Attack setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1106 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:under_attack"

  @behavior:setup:encounter-set:legions_of_hydra:setup-encounters
  @setup:encounter-set:legions_of_hydra
  Scenario: The Legions of Hydra setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1107 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:legions_of_hydra"

  @behavior:setup:encounter-set:the_doomsday_chair:setup-encounters
  @setup:encounter-set:the_doomsday_chair
  Scenario: The Doomsday Chair setup record contributes its complete encounter set
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets       | seed |
      | rhino    | spider_man | the_doomsday_chair | 1108 |
    When the dealt Core scene is inspected
    Then the dealt scene matches setup authority "setup:encounter-set:the_doomsday_chair"
