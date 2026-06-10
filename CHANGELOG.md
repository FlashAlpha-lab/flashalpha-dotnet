# Changelog

All notable changes to the FlashAlpha .NET SDK are documented here.

## 1.1.0 - 2026-06-08

Large endpoint-parity release. Adds full coverage of the new FlashAlpha API
families, each with an untyped `*Async` (returning `Task<JsonElement>`) and a
strongly-typed `*TypedAsync` variant backed by a `*Response` POCO. All existing
method signatures remain backward-compatible — new parameters are optional.

### Added — Strategy Signals (10 endpoints, shared `StrategyDecisionResponse`)

- `StrategyFlowAnomalyAsync` — directional options-flow anomaly score + short vertical (Growth+).
- `StrategyExpiryPositioningAsync` — OPEX pin-risk score + iron-fly proposal (Basic+).
- `StrategyZeroDteAsync` — 0DTE intraday structure proposal (Growth+, 0DTE entitlement).
- `StrategyDealerRegimeAsync` — positive vs negative gamma dealer-regime read (Growth+).
- `StrategyVolCarryAsync` — vol-carry harvesting candidate (Alpha+).
- `StrategyYieldEnhancementAsync` — covered-call / cash-secured-put overlay (Growth+).
- `StrategySurfaceAnomalyAsync` — surface-anomaly / vol-arb signal (Alpha+).
- `StrategySkewAsync` — risk-reversal / put-skew richness signal (Growth+).
- `StrategyTermStructureAsync` — calendar / diagonal opportunity signal (Growth+).
- `StrategyTailPricingAsync` — wing richness / cheap-convexity signal (Growth+).

### Added — Earnings analytics

- `EarningsCalendarAsync` — forward earnings calendar (Growth+).
- `EarningsExpectedMoveAsync` — earnings-implied straddle move (Growth+).
- `EarningsHistoryAsync` — historical implied-vs-realized reactions and surprises (Growth+).
- `EarningsIvCrushAsync` — estimated post-earnings IV crush (Growth+).
- `EarningsVrpAsync` — earnings variance risk premium (Alpha+).
- `EarningsDealerPositioningAsync` — dealer GEX positioning into the event (Alpha+).
- `EarningsStrategiesAsync` — earnings-aware strategy scores (straddle/strangle/iron-condor) (Alpha+).
- `EarningsScreenerAsync` — cross-sectional earnings screener (VRP richest / cheapest move / highest crush / importance) (Alpha+).

### Added — Structures (pure-math POST)

- `StructurePnlAsync` — at-expiry P&L curve, breakevens, max profit/loss for a multi-leg structure (Basic+).
- `StructureGreeksAsync` — aggregated, quantity-scaled, direction-signed position Greeks (Basic+).

### Added — Zero-DTE Flow (intraday)

- `FlowZeroDteSnapshotAsync` — live 0DTE flow snapshot (`ZeroDteFlowSnapshotResponse`, derives from `ZeroDteResponse`) (Growth+).
- `FlowZeroDteSeriesAsync` — intraday 0DTE flow time series (Growth+).
- `FlowZeroDteHedgeFlowAsync` — estimated dealer hedge-flow series (Growth+).
- `FlowZeroDteHeatmapAsync` — strike × time 0DTE heatmap matrix (Alpha+).
- `FlowZeroDteStrikeFlowAsync` — per-strike signed aggressor flow (Alpha+).

### Added — Exposure analytics (extended)

- `ExposureSheetAsync` — per-strike GEX/DEX/VEX/CHEX dealer exposure sheet with totals, levels, peaks (Growth+).
- `ExposureTermStructureAsync` — net exposure broken out by expiry bucket (Growth+).
- `ExposureBasketAsync` — weighted cross-symbol exposure aggregate (up to 50 symbols) (Growth+).
- `ExposureOiDiffAsync` — largest open-interest changes since the prior snapshot (Growth+).

### Added — Volatility analytics (additional)

- `LiquidityAsync` — per-expiry execution / liquidity score (Growth+).
- `SkewTermAsync` — skew + term-structure of implied volatility (25-delta risk reversals, ATM term curve) (Growth+).
- `SpotVolCorrelationAsync` — spot–vol correlation / leverage-effect regime (Growth+).
- `DispersionAsync` — implied vs realized correlation (dispersion / vol-arb) for an index versus a basket (Alpha+).
- `ExpectedMoveAsync` — options-implied expected move (straddle-derived) per expiry (Basic+).
- `VrpHistoryAsync` — historical VRP time series (Alpha+).

### Added — Macro / Universe / Surface

- `VixStateAsync` — VIX term-structure regime snapshot (contango/backwardation, VIX/VIX3M, percentiles) (Growth+).
- `UniverseAsync` — curated tier-1 / tier-2 pre-warmed symbol universe (Public).
- `SurfaceSviAsync` — calibrated SVI surface parameters per expiry slice (Alpha+).

### Added — Flow (extended) & Screener

- `FlowDealerPremiumAsync` — net dealer premium roll-up over the full tape (VWAP-weighted) (Alpha+).
- `FlowStockBarsAsync` — multi-resolution OHLCV + flow bars from the live trade tape (Alpha+).
- `ScreenerFieldsAsync` — catalogue of screener fields available for filters/sort/select/formulas (Free+).

### Changed

- `ZeroDteAsync` / `ZeroDteTypedAsync` — added optional `expiry` parameter to target a specific 0DTE expiry.
- `VrpAsync` / `VrpTypedAsync` — added optional `date` parameter for point-in-time VRP.
- `ZeroDteResponse` is no longer `sealed` so `ZeroDteFlowSnapshotResponse` can derive from it.

## 1.0.1

- Maintenance release.

## 1.0.0

- Initial public release of the FlashAlpha .NET SDK.
</content>
</invoke>
