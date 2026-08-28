# AqariOS — Plans, Subscriptions & Plan Change Requests
## Business Rules Specification

**Status:** Business Rules Baseline  
**Purpose:** Authoritative business rules for backend implementation  
**Scope:** Platform Admin, Company Admin, Plans, Company Subscriptions, Plan Change Requests  
**Important:** This document defines the intended business behavior even where the current backend does not yet implement it.

---

## 1. Core Business Principle

AqariOS separates three concepts:

1. **Plan** — a commercial product definition offered by the platform.
2. **Subscription** — a company's actual subscription to a specific Plan.
3. **Plan Change Request** — a request from a Company Admin to move an existing Subscription to another Plan or Billing Cycle.

A Company Admin does not directly change a subscription.

The Platform Admin is the authority responsible for approving and executing subscription changes.

---

# 2. Plans

## 2.1 Plan Definition

A Plan represents a specific commercial version of an AqariOS offering.

A Plan contains its own:

- Name
- Arabic/English display names
- Description
- Monthly price
- Yearly price
- Currency
- Limits
- Features
- Trial configuration
- Ordering
- Active/inactive status

Once a Plan has been used by a subscription, its commercial definition is considered **immutable**.

---

## 2.2 Plan Immutability

A used Plan must not be edited in place.

The following are considered commercial properties and must not be changed on a used Plan:

- Monthly price
- Yearly price
- Currency
- Limits
- Features
- Trial configuration
- Commercial display information where changing it would alter the historical meaning of the Plan

If a commercial change is required:

1. The existing Plan is deactivated.
2. A new Plan is created.
3. The new Plan receives a new unique ID.
4. New companies see only the new active Plan.
5. Existing subscriptions remain attached to the old Plan.
6. Existing companies do not move automatically.

---

## 2.3 Plan Versioning by Identity

A new commercial version is represented by a new Plan record/ID.

The system does not need to expose technical version labels such as `v1` or `v2` to customers.

Example:

```text
Plan ID A
Pro
25 JOD / month
10 Buildings
INACTIVE

Plan ID B
Pro
30 JOD / month
20 Buildings
ACTIVE
```

Both may have the same commercial display name, but they are different Plan records.

---

## 2.4 Plan Activation

Only active Plans are available for new subscriptions.

An inactive Plan:

- Must not appear in the normal available Plans catalog.
- Must not be selectable for a new subscription.
- Must remain readable for historical subscriptions.
- Must remain associated with existing subscriptions that already use it.

---

## 2.5 Plan Deactivation

Deactivating a Plan does not modify existing subscriptions.

If Company A is subscribed to Plan A and Plan A becomes inactive:

```text
Company A
    ↓
Subscription
    ↓
Plan A (Inactive)
```

Company A remains subscribed to Plan A.

The subscription does not automatically move to another Plan.

---

## 2.6 Plan Deletion

Plans should not be hard-deleted once they are referenced by a subscription.

A Plan with historical or active subscription references must be retained.

The preferred lifecycle is:

```text
Active
  ↓
Inactive
```

not:

```text
Active
  ↓
Deleted
  ↓
Replacement
```

Deletion of an unused Plan may be considered separately, but it is not part of the normal production lifecycle.

---

# 3. New Company Plan Visibility

A Company Admin viewing available Plans must see only Plans that are currently active.

Inactive Plans are not part of the purchasing catalog.

However, if the Company already has a subscription to an inactive Plan, the Company Admin must still be able to see the current subscription and its Plan information.

Therefore:

**Available Plans != Historical/Current Subscription Plans**

---

# 4. Company Subscription

## 4.1 Subscription Ownership

A Subscription belongs to a Company.

The Subscription references one specific Plan by Plan ID.

The relationship is important because it preserves which exact commercial Plan the Company subscribed to.

---

## 4.2 Existing Subscription Stability

Deactivating or replacing a Plan must not automatically modify existing subscriptions.

Example:

```text
Old Plan
25 JOD
10 Buildings
Inactive

Company A
    ↓
Subscription → Old Plan
```

Company A remains on the old Plan.

A new company:

```text
New Company
    ↓
Subscription → New Plan
```

uses the new active Plan.

---

## 4.3 No Automatic Migration

Creating a replacement Plan must never automatically migrate existing companies.

There is no implicit:

```text
Old Plan → New Plan
```

migration.

A Company must explicitly request a change.

---

# 5. Company Admin Subscription Access

A Company Admin may:

- View the current subscription.
- View the current Plan.
- View the current Billing Cycle.
- View subscription status.
- View relevant subscription dates.
- View available active Plans.
- Request a Plan change.

A Company Admin may not directly:

- Change the Plan.
- Change the Billing Cycle.
- Activate a different Plan.
- Cancel the current subscription through the Plan Change workflow.
- Modify subscription commercial terms directly.

---

# 6. Plan Change Request

## 6.1 Purpose

A Plan Change Request is the controlled workflow for changing a company's existing subscription.

The request separates:

**Customer intent**

from

**Platform Admin approval and execution**.

---

## 6.2 Request Flow

```text
Company Admin
      ↓
Selects desired Plan
      ↓
Selects desired Billing Cycle
      ↓
Submits Request
      ↓
PENDING
      ↓
Platform Admin Review
      ├── APPROVE
      └── REJECT
```

The Company Admin does not directly modify the Subscription.

---

# 7. Plan Change Request Data

A request should preserve at minimum:

- Request ID
- Company ID
- Current Subscription ID
- Current Plan ID
- Requested Plan ID
- Current Billing Cycle
- Requested Billing Cycle
- Requested By
- Requested At
- Request Status
- Admin Reviewer
- Reviewed At
- Admin Note / Decision Note
- Rejection Reason where applicable

The request should preserve enough information to determine what the Company requested at the time the request was submitted.

---

# 8. Plan Change Request Statuses

The initial status set is:

```text
Pending
Approved
Rejected
Cancelled
```

## Pending

The request has been submitted and has not yet received a final decision.

## Approved

The Platform Admin approved the request.

The actual Subscription change must then be applied according to the applicable Plan Change rules.

## Rejected

The Platform Admin rejected the request.

A rejection reason should be stored.

The Subscription remains unchanged.

## Cancelled

The Company Admin cancelled the request before it was reviewed.

---

# 9. Pending Request Rules

A Company may have only **one Pending Plan Change Request** at a time.

A Company cannot create another Pending request while one already exists.

This prevents conflicting requests such as:

```text
Basic → Pro
Basic → Enterprise
Basic → Pro Yearly
```

all being pending simultaneously.

---

# 10. Cancelling a Request

A Company Admin may cancel a request only while it is:

```text
Pending
```

Once a request is:

```text
Approved
Rejected
```

it cannot be cancelled.

---

# 11. Duplicate Request Prevention

A Company cannot request the exact same:

```text
Plan + Billing Cycle
```

that it is already subscribed to.

Example:

```text
Current:
Pro / Monthly

Request:
Pro / Monthly
```

must be rejected.

However:

```text
Pro / Monthly
        ↓
Pro / Yearly
```

is a valid change request because the Billing Cycle changes.

---

# 12. Billing Cycle Changes

Supported Billing Cycles are determined by the platform's subscription model.

The current intended model is:

```text
Monthly
Yearly
```

Changing Billing Cycle is considered a commercial subscription change and therefore requires Platform Admin approval.

Examples:

```text
Monthly → Yearly
Yearly → Monthly
```

must not happen automatically from the Company Admin UI.

---

# 13. Upgrade Rules

An Upgrade is a move to a commercially higher Plan.

The exact definition of "higher" should be determined by an explicit Plan ordering/ranking rule rather than inferred from price alone.

After Platform Admin approval:

- The new Plan may become effective immediately according to the final billing implementation.
- The old Plan remains available as historical data.
- The Subscription changes to reference the new Plan.
- The change must be auditable.

The exact financial/proration behavior is intentionally not defined by this document until the payment/billing model is finalized.

---

# 14. Downgrade Rules

A Downgrade is a move to a commercially lower Plan.

After approval, the downgrade should normally take effect at the end of the current paid billing period rather than immediately removing benefits the Company has already paid for.

The system should therefore support the concept of a scheduled subscription change if the billing implementation requires it.

The exact proration/refund behavior is not defined until the payment model is finalized.

---

# 15. Plan Change Approval

The Platform Admin is the authority that approves or rejects Plan Change Requests.

Approval must not silently modify unrelated subscription information.

Approval should:

1. Verify the request is still valid.
2. Verify the requested Plan is still active/eligible.
3. Verify the current Subscription has not changed in a conflicting way.
4. Apply the approved change according to billing rules.
5. Record the reviewer and decision.
6. Record the time of the decision.
7. Produce an audit entry.

---

# 16. Rejection

When rejecting a Plan Change Request:

- The request becomes `Rejected`.
- The Subscription remains unchanged.
- A rejection reason should be recorded.
- The Company Admin should be able to see the request result.
- The decision must be auditable.

---

# 17. Requested Plan Becomes Inactive

If a requested Plan becomes inactive while a request is still Pending:

The request must not result in a subscription to the inactive Plan.

The Platform Admin must either:

- Reject the request, or
- Resolve it using another valid active Plan through an explicit controlled workflow.

The system must not silently substitute another Plan.

---

# 18. Plan Pricing

A Plan's official pricing belongs to the Plan definition.

A new price should be represented by a new Plan rather than changing the price of a Plan already used by customers.

Example:

```text
Old Plan
Pro
25 JOD
Inactive

New Plan
Pro
30 JOD
Active
```

Existing customers remain on the old Plan unless they explicitly change their subscription.

---

# 19. Subscription Price History

The Subscription should preserve the commercial price applicable when the subscription was established or changed.

The implementation must ensure that historical subscription pricing does not unexpectedly change merely because a new Plan is created.

Any future billing implementation must define exactly how:

- Subscription price
- Plan price
- Invoice price
- Discount
- Custom pricing

relate to one another.

---

# 20. Plan Feature and Limit Changes

Limits and Features are treated as part of the Plan's commercial definition.

Therefore:

Changing:

- Max Buildings
- Max Users
- Storage
- Feature availability
- Quotas

for an already-used Plan should be represented by creating a replacement Plan.

Existing subscriptions remain on their original Plan.

---

# 21. Platform Admin Subscription Creation

The Platform Admin should be able to create a Subscription for a Company through the Platform Admin area.

The intended flow is:

```text
Platform Admin
      ↓
Select Company
      ↓
Select Active Plan
      ↓
Select Billing Cycle
      ↓
Configure allowed subscription parameters
      ↓
Create Subscription
```

The Platform Admin must not create a subscription against an inactive Plan.

The exact fields available for manual creation must follow the final Subscription API and billing rules.

---

# 22. One Active Subscription Per Company

A Company should have at most one active/current subscription at a time.

The system must prevent conflicting active subscriptions for the same Company.

Historical subscriptions may exist as historical records if the final subscription lifecycle supports them.

---

# 23. Subscription Status vs Request Status

These are separate concepts.

### Subscription Status

Represents the state of the actual Subscription.

Example statuses may include:

```text
Trialing
Active
PastDue
Suspended
Cancelled
Expired
```

### Plan Change Request Status

Represents the state of a request:

```text
Pending
Approved
Rejected
Cancelled
```

A rejected request does not mean the Subscription is rejected.

Example:

```text
Plan Change Request = Rejected
Subscription = Active
```

---

# 24. Audit Requirements

The following actions should be auditable:

- Plan creation
- Plan activation
- Plan deactivation
- Subscription creation
- Plan Change Request creation
- Request cancellation
- Request approval
- Request rejection
- Subscription Plan change
- Subscription lifecycle changes

For decisions made by Platform Admin, the audit record should identify:

- Actor
- Action
- Target
- Timestamp
- Relevant before/after values where appropriate
- Decision/rejection reason where applicable

---

# 25. Notifications

The final system should notify Company Admins about important Plan Change Request outcomes.

At minimum:

- Request submitted
- Request approved
- Request rejected

The exact notification channels are subject to the AqariOS Notifications module.

---

# 26. Business Rules That Are Intentionally NOT Defined Yet

The following must not be invented during implementation:

- Payment gateway behavior
- Proration
- Refund calculations
- Invoice generation
- Exact renewal billing logic
- Grace period duration
- Past-due transition timing
- Automatic expiration timing
- Automatic renewal processing
- Payment retry policy
- Custom pricing
- Discounts
- Tax handling
- Currency conversion

These require a separate Billing/Financial Operations decision.

---

# 27. Non-Negotiable Rules

The following are core rules for implementation:

1. A used Plan is not edited in place.
2. A used Plan is not hard-deleted.
3. Commercial Plan changes create a new Plan.
4. Old Plans are deactivated, not deleted.
5. Existing subscriptions remain attached to their original Plan.
6. New companies see only active Plans.
7. Creating a new Plan does not migrate existing subscriptions.
8. Company Admin cannot directly change the Subscription Plan.
9. Company Admin submits a Plan Change Request instead.
10. Only one Pending Plan Change Request is allowed per Company.
11. Company Admin can cancel only Pending requests.
12. Platform Admin approves or rejects requests.
13. Rejection should include a reason.
14. The requested Plan must be valid and active when the change is executed.
15. Plan Change Request status and Subscription status are separate.
16. Subscription history must remain explainable after Plan changes.
17. All commercial subscription decisions must be auditable.
18. Billing-specific behavior must not be invented until the billing model is finalized.

---

# 28. Reference Lifecycle

The intended high-level lifecycle is:

```text
                    PLAN
                     │
             ┌───────┴────────┐
             │                │
          ACTIVE           INACTIVE
             │                │
             │          Historical only
             │
             ▼
       New Subscription
             │
             ▼
        COMPANY SUBSCRIPTION
             │
             │
       Company wants change
             │
             ▼
      PLAN CHANGE REQUEST
             │
        ┌────┴─────┐
        │          │
     APPROVE     REJECT
        │          │
        ▼          ▼
 Subscription    Subscription
 changes         unchanged
```

---

# 29. Implementation Principle

Backend implementation must follow this document as the intended Business Rules baseline.

Where the current backend conflicts with these rules, the current backend must be treated as **not yet aligned** rather than as the desired behavior.

The implementation should be completed in the following order:

1. Database/domain model reconciliation.
2. Plan lifecycle implementation.
3. Platform Admin Plan APIs.
4. Platform Admin Subscription APIs.
5. Plan Change Request domain model.
6. Plan Change Request application logic.
7. Authorization/RBAC.
8. Audit integration.
9. API contracts.
10. Manual backend verification.
11. Frontend implementation based on the finalized API contracts.

---

## Final Business Decision

AqariOS will use an **immutable commercial Plan model**.

A Plan that has been used by a Subscription is never edited or hard-deleted.

When AqariOS changes the commercial definition of a Plan:

```text
Old Plan → INACTIVE
New Plan → ACTIVE
```

Existing companies remain on the Old Plan.

New companies can subscribe only to the New Active Plan.

Existing companies may request migration to the New Plan, but the migration requires Platform Admin approval.

The Company Admin is therefore a consumer of the subscription system, not the final authority over commercial subscription changes.
