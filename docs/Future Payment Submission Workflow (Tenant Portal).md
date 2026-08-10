# AqariOS — Future Payment Submission Workflow (Tenant Portal)

> **Status:** Future Enhancement (Post-MVP)
>
> This document describes the future payment workflow between the Tenant Portal and the Owner/Property Manager Portal. It is intentionally designed to extend the current Module 6 architecture without replacing or breaking the existing payment engine.

---

# Objective

Introduce a controlled payment submission workflow where:

- The tenant can notify the owner that a payment has been made.
- The owner remains the only party authorized to officially confirm the payment.
- AqariOS automatically generates an official receipt after payment verification.
- No payment is considered completed until verified by the owner.

This preserves financial integrity while providing a modern tenant experience.

---

# Payment Lifecycle

```
Pending
      │
      ▼
Payment Submitted
      │
 ┌────┴────┐
 │         │
 ▼         ▼
Verified   Rejected
(Paid)
```

---

# Payment Status Definitions

## Pending

The payment obligation exists but the tenant has not taken any action.

Examples:

- Monthly rent
- Outstanding balance
- Scheduled installment

---

## Payment Submitted

The tenant claims that payment has been made.

Examples:

- Cash handed to property manager
- CliQ transfer submitted
- Bank transfer completed
- Cheque delivered

At this stage:

- The payment is NOT considered paid.
- Financial balances remain unchanged.
- The payment is waiting for owner verification.

---

## Verified (Paid)

The owner verifies that payment has actually been received.

System actions:

- Update Rent Payment status.
- Allocate payment.
- Generate official receipt.
- Notify tenant.
- Update dashboards and reports.

---

## Rejected

Owner could not verify the payment.

Examples:

- Money not received.
- Wrong amount.
- Invalid transfer reference.
- Invalid cheque.

Tenant is notified with the rejection reason.

---

# Tenant Workflow

---

## Step 1

Tenant opens:

My Payments

Example:

```
August Rent

300 JOD

Status:
Pending
```

---

## Step 2

Tenant clicks:

```
Pay
```

---

## Step 3

Tenant selects payment method.

Available methods:

- Cash
- CliQ
- Bank Transfer
- Cheque

---

# Cash Workflow

Tenant sees:

> Please hand the payment directly to the property manager.

Tenant presses:

```
I Have Handed Over The Cash
```

Result:

Payment Status

```
Payment Submitted
```

No money is officially recorded yet.

---

# CliQ Workflow

Tenant sees:

- Recipient Name
- CliQ ID
- Amount

Tenant performs the transfer.

After completing the transfer:

Required fields:

- Reference Number

Required attachment:

- Screenshot of successful payment

Tenant presses:

```
Submit Payment
```

Status becomes:

```
Payment Submitted
```

---

# Bank Transfer Workflow

Tenant enters:

- Reference Number

Uploads:

- Bank transfer receipt

Status:

```
Payment Submitted
```

---

# Cheque Workflow

Tenant enters:

- Cheque Number
- Bank Name
- Due Date

Optional:

- Photo of cheque

Status:

```
Payment Submitted
```

---

# Attachment Rules

| Payment Method | Attachment Required |
|----------------|---------------------|
| Cash | No |
| CliQ | Yes |
| Bank Transfer | Yes |
| Cheque | Optional |

---

# Owner Workflow

Owner opens:

Payments Workspace

A new section appears:

```
Waiting For Verification
```

Each payment request displays:

- Tenant Name
- Building
- Apartment
- Contract Number
- Amount
- Payment Method
- Submission Date
- Reference Number (if available)
- Attachment Preview

---

# Owner Decision

Owner has only two actions.

## Approve

Owner confirms payment has been received.

System automatically:

- Marks payment as Paid
- Allocates payment
- Updates balances
- Generates receipt
- Stores receipt
- Notifies tenant

---

## Reject

Owner rejects the submission.

Owner provides:

- Rejection reason

Tenant receives notification.

Payment returns to:

```
Pending
```

---

# Receipt Generation

The owner should NEVER upload a receipt manually.

Instead, AqariOS automatically generates an official PDF receipt after approval.

Generated automatically:

- Receipt Number
- Payment Amount
- Payment Date
- Tenant
- Lease Contract
- Property
- Payment Method

The PDF is stored in the Documents module and becomes available to both parties.

---

# Tenant Portal

After approval the tenant can:

- View payment history
- Download receipt
- View payment status
- View rejection reason (if rejected)

---

# Security Principles

The tenant is NOT allowed to:

- Mark payments as Paid.
- Modify payment status.
- Generate receipts.
- Alter financial balances.

The tenant can only:

- Submit a payment notification.
- Upload supporting evidence.
- Track payment status.

---

# Owner Authority

Only the owner (or authorized company staff) can:

- Verify payments.
- Reject payments.
- Generate official receipts.
- Affect financial records.

---

# Architectural Principles

This enhancement must:

- Build on top of the existing Module 6 backend.
- Preserve the current payment engine.
- Avoid replacing existing payment entities.
- Introduce a verification layer rather than changing financial logic.
- Keep AqariOS as the single source of truth for payment confirmation.

---

# Expected Benefits

- Prevent fraudulent payment confirmations.
- Maintain complete auditability.
- Reduce payment disputes.
- Improve tenant experience.
- Preserve accounting integrity.
- Automate receipt generation.
- Scale cleanly for future online payment gateway integrations.