# Meta-Loop Systems

## Runtime Service
- `Assets/Scripts/Economy/MetaLoopRuntimeService.cs`

## Implemented Subsystems
### Rotating Contracts
- Active contract is persisted as objective-progress entry `meta_contract_active`.
- Contract target fish rotates and grants copec reward on completion.
- Progress updates on landed catches.

### Collection Bonus
- Collection set tracks unique fish catches (tokenized in progression unlock ids).
- Completing the set grants one-time collection reward.

### Market Demand Modifiers
- Fish-specific demand multiplier is day-sensitive and deterministic.
- Sell value pipeline applies demand multiplier in `SellSummaryCalculator`.

### Gear Synergy Bonuses
- Configurable ship/hook synergy table with sell multipliers.
- Sell value pipeline applies synergy multiplier when matched loadout is equipped.

## Integration Points
- `SellSummaryCalculator` applies:
  - distance tier multiplier
  - market demand multiplier
  - gear synergy multiplier
- `CatchResolver` HUD condition line appends active demand/synergy modifier context.
- `ProfileMenuController` surfaces:
  - active contract status
  - collection progress/reward state
  - demand summary

## Persistence
- Contract state is persisted in `SaveDataV1.objectiveProgress.entries`.
- Collection tokens are persisted in `SaveDataV1.progression.unlockedContentIds`.

## Validation Coverage
- `Assets/Tests/EditMode/MetaLoopRuntimeServiceTests.cs`
