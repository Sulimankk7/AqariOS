Design a production-ready Authentication Workspace for AqariOS, an enterprise Property Management ERP.

IMPORTANT

This is NOT a marketing website.

This is NOT a real estate listing platform.

This is an internal enterprise SaaS application used daily by professional property management companies.

The final result should feel handcrafted by a senior product designer.

--------------------------------------------------------
DESIGN PHILOSOPHY
--------------------------------------------------------

• Backend-first UI

Every UI element must correspond to an actual backend function.

Do not invent features.

• Function over decoration

Prioritize workflow, hierarchy and usability.

• Minimal but not empty.

Use whitespace intentionally.

• Enterprise SaaS.

Inspired by:

- Linear
- GitHub
- Clerk
- Stripe Dashboard
- Vercel Dashboard
- Notion

Quiet.

Professional.

Premium.

Timeless.

--------------------------------------------------------
COLOR PALETTE
--------------------------------------------------------

Use ONLY these colors.

Dark Green
#333D29

Primary Green
#414833

Secondary Green
#656D4A

Soft Sage
#A4AC86

Light Sage
#C2C5AA

Dark Brown Accent
#582F0E

Brown Accent
#7F4F24

Warm Accent
#936639

Soft Beige
#A68A64

Neutral Beige
#B6AD90

Background
#FAFAFA

Surface
#FFFFFF

Border
#E5E7EB

Primary Text
#111827

Secondary Text
#6B7280

Greens are the primary colors.

Brown should only be used as subtle accents.

--------------------------------------------------------
GENERAL LAYOUT
--------------------------------------------------------

Authentication is NOT multiple disconnected pages.

It is one shared authentication workspace.

The overall layout NEVER changes.

Desktop:

Left panel:
40%

Right panel:
60%

--------------------------------------------------------
LEFT PANEL
--------------------------------------------------------

Keep it identical across all authentication screens.

Never move it.

Never replace it.

Include:

Small AqariOS logo

Property Management Platform

Large whitespace

Very subtle architectural line illustration.

The illustration should use the same style as the Login page.

Thin outline only.

Very light sage color.

No photos.

No 3D.

No hero image.

No marketing artwork.

No statistics.

No feature cards.

The left side should remain visually stable during navigation.

--------------------------------------------------------
RIGHT PANEL
--------------------------------------------------------

Contains only the authentication content.

No floating oversized card.

Instead use an integrated panel with subtle border and shadow.

Large spacing.

--------------------------------------------------------
AUTHENTICATION FLOW
--------------------------------------------------------

Everything happens inside the SAME layout.

Do NOT navigate to different pages.

Use smooth transitions between:

Login

↓

Register

↓

Phone OTP Login

↓

OTP Verification

↓

Forgot Password

↓

Reset Password

Only the form changes.

Everything else remains fixed.

--------------------------------------------------------
ANIMATION
--------------------------------------------------------

Professional enterprise animation.

NOT flashy.

The left branding panel never moves.

The footer never moves.

Language selector never moves.

Only the authentication form transitions.

Animation:

Fade

+

Small horizontal slide (16-24px)

Duration:

250-300ms

Use easing similar to Clerk or Linear.

The transition should feel like changing steps inside one workflow.

Never swap left and right panels.

--------------------------------------------------------
LOGIN
--------------------------------------------------------

Include:

Email

Password

Show password

Remember me

Forgot password

Sign In

Continue with Google

Phone OTP Login

Language switch

--------------------------------------------------------
REGISTER
--------------------------------------------------------

Must exactly match the backend.

Fields:

Company Name

Display Name (Optional)

Company Type

Full Name

Email

Phone Number

Country Code

Password

Confirm Password (UI validation only)

Preferred Language

Terms & Conditions

Create Account

Continue with Google

Already have an account? Sign In

DO NOT ADD

Username

Address

City

Tax Number

Logo Upload

Website

Industry

--------------------------------------------------------
PHONE OTP LOGIN
--------------------------------------------------------

This application uses PHONE OTP.

NOT Email OTP.

Screen 1

Phone Number

Country Code

Send Verification Code

After successful request

Automatically transition to

OTP Verification

--------------------------------------------------------
OTP VERIFICATION
--------------------------------------------------------

Use backend flow.

Fields:

Phone Number (readonly)

6-digit OTP input

Verify Code

Resend Code

Countdown Timer

Change Phone Number

Do NOT ask for email.

--------------------------------------------------------
FORGOT PASSWORD
--------------------------------------------------------

Support both recovery methods if backend allows:

Email

Phone OTP

Keep same layout.

--------------------------------------------------------
VISUAL STYLE
--------------------------------------------------------

Inter

10-12px radius

8pt spacing system

Soft shadows

Thin borders

Minimal icons

Perfect alignment

Strong typography hierarchy

No visual clutter

No decorative graphics

No fake dashboard

No marketing sections

No unnecessary illustrations

--------------------------------------------------------
RESPONSIVE
--------------------------------------------------------

Desktop

Tablet

Mobile

--------------------------------------------------------
ACCESSIBILITY
--------------------------------------------------------

Keyboard navigation

Visible focus states

High contrast

Large click targets

--------------------------------------------------------
OVERALL GOAL
--------------------------------------------------------

The entire authentication experience should feel like one premium enterprise workspace.

Users should never feel they are leaving the page.

Everything should feel calm, polished and production-ready.

The authentication flow should resemble modern SaaS products like Clerk, Linear or GitHub rather than a traditional website.