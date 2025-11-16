## Problems Observed
- Low contrast: foreground text and backgrounds (e.g., #0f1535) too similar.
- Chart readability: legend/axes colors and tooltip formatting not tuned for dark UI.
- Revenue signals: currency formatting and color semantics not aligned, inconsistent accents.

## Research Summary
- WCAG 2.1 contrast for body text ≥ 4.5:1; large text ≥ 3:1 (Concrete CMS article)
- Use accessible palette generators to derive compliant pairs (Venngage generator; Color Safe)
- Building palettes with perceptual lightness (CIELAB/LCh) yields consistent contrast (Accessible Palette)
- InclusiveColors guidance: plan pairs (background → heading/body/form/label) and check APCA/WCAG while editing

## Proposed Color System (Dark Background #0f1535)
- Background: `#0f1535` (primary backdrop)
- Surface: `#111a3f` (cards/panels), `#141a47` (hover/active)
- Borders: `rgba(255,255,255,0.06)` subtle hairlines
- Text:
  - Primary: `#E6EEFA` (≈ light slate) – AA contrast over #0f1535
  - Secondary: `#A7B3CC`
  - Muted: `#7D8AA8`
- Accents (compliant against dark bg):
  - Indigo: `#4F46E5` → cyan: `#06B6D4` (gradients)
  - Success: `#22C55E`
  - Warning: `#F59E0B`
  - Danger: `#EF4444`
- Chart palette (fills/lines on dark):
  - Realized: `#22C55E`
  - Quoted: `#60A5FA`
  - Gridlines/ticks: `rgba(255,255,255,0.08)` / labels `#94A3B8`

## Implementation Steps
1. Theme Tokens
- Create CSS variables (e.g., :root[data-theme="dark"]) for bg, surface, border, text-primary/secondary/muted, accent-success/warning/danger/indigo/cyan.
- Reference these in Tailwind via `theme.extend.colors` or use CSS variables directly in classes for consistency.

2. Global UI Updates
- Apply background `#0f1535` to app shell only (body/html).
- Change panels to surface `#111a3f` and hover `#141a47`.
- Update text colors to `text-primary`/`text-secondary`; ensure headings/body meet ≥ 4.5:1.
- Update border utilities to use subtle hairline (`rgba(255,255,255,0.06)`).

3. Sales Dashboard Adjustments
- KPI tiles: use gradient icon badges; values with `text-primary`, labels with `text-secondary`.
- Revenue chart:
  - Currency format in tooltips and y-axis (e.g., $12,890.00)
  - Dark axis/grid styling per chart palette
- Status doughnut: ensure registered controller; use explicit legend `#CBD5E1`.

4. Accessibility Validation
- Use Venngage/Color Safe to check contrast for text-on-bg pairs (≥ 4.5:1) and large text (≥ 3:1).
- Verify headings/body/forms/labels on surfaces meet AA; increase brightness of text if any pair fails.
- Optionally test APCA via InclusiveColors for future-proofing.

5. Revenue UX Refinement
- Add delta badges (+15%) using success/danger colors
- Add period toggle (Week/Month/Year) with accessible button contrast
- Ensure requests/pipeline tables adopt new surface/text colors and row hover states

6. Rollout & QA
- Update theme tokens; refactor Sales pages and shared components to tokens
- Run UI build; test dark theme across pages
- Validate contrast via tooling, fix outliers

## Deliverables
- Color tokens and Tailwind/CSS integration
- Updated Sales dashboard with accessible colors and chart styling
- Contrast validation report (tool outputs/screenshots)

## Approval
- Confirm the proposed palette and steps. On approval, I’ll implement tokens, update components, fix charts/tooltips, and provide a quick visual QA checklist with contrast scores.