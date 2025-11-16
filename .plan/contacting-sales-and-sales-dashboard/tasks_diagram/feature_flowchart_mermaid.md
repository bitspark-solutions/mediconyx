```mermaid
flowchart TB
  subgraph PublicFlow[Contact Sales]
    CSF[Contact Sales Form]
    CAPT[Captcha Verify]
    POSTP[POST /api/sales/requests/public]
    PENDING[Status = pending]
    EMAIL[Notify Sales]
    CSF --> CAPT --> POSTP --> PENDING --> EMAIL
  end

  subgraph SalesAuth[Sales Login]
    LOGIN[POST /api/auth/login]
    TOKENS[Issue JWT tokens]
    DASH[Load Sales Dashboard]
    LOGIN --> TOKENS --> DASH
  end

  subgraph Metrics[Metrics & Trends]
    KPIS[GET /api/sales/metrics/kpis]
    TRENDS[GET /api/sales/metrics/trends]
    REVENUE[GET /api/sales/metrics/revenue]
    KPIS --> TRENDS
    KPIS --> REVENUE
  end

  subgraph Pipeline[Pipeline Management]
    LIST[GET /api/sales/requests]
    CLAIM[POST /api/sales/requests/{id}/claim]
    UNASSIGN[POST /api/sales/requests/{id}/unassign]
    STATUS[PATCH /api/sales/requests/{id}/status]
    NOTES[POST /api/sales/requests/{id}/notes]
    FILES[POST /api/sales/requests/{id}/attachments]
    LIST --> CLAIM --> STATUS --> NOTES
    CLAIM --> UNASSIGN
    STATUS --> FILES
  end

  subgraph Pricing[Pricing & Payment]
    PRICING[PATCH /api/sales/requests/{id}/pricing]
    PAYMENT[PATCH /api/sales/requests/{id}/payment-status]
    PRICING --> PAYMENT
  end

  subgraph Admin[Master Admin Sales Users]
    CREATEU[POST /api/admin/sales-users]
    LISTU[GET /api/admin/sales-users]
    ACTU[PATCH /api/admin/sales-users/{id}]
    CREATEU --> LISTU --> ACTU
  end

  CSF --> SalesAuth
  DASH --> Metrics
  DASH --> Pipeline
  DASH --> Pricing
  Admin --> Pipeline
  STATUS --> KPIS
  STATUS --> TRENDS
  PAYMENT --> REVENUE
```
