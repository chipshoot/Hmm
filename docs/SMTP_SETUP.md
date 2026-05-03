# SMTP Setup for Hmm.Idp

How email-sending is wired in each environment, what to do once before
production registrations work, and how to debug when delivery fails.

---

## TL;DR

| Environment | SMTP host | Port | Auth | Where mail lands |
|---|---|---|---|---|
| **Test Docker** (`./docker/test-env.sh`) | `mailpit` (container) | `1025` | none | Mailpit UI at <http://localhost:8025> — never leaves your machine |
| **VPS production** (Oracle Cloud) | `mail.homemademessage.com` (AspHostPortal) | `587` (STARTTLS) | mailbox creds | Real recipient inbox |

Both wires read the same `EmailSettings` config block; only the values differ.

---

## How the IDP picks up SMTP config

`Hmm.Idp/Services/EmailService.cs` constructor-injects
`IOptions<EmailSettings>`. The settings class is bound from configuration —
which, thanks to ASP.NET Core's default config sources, accepts values from
`appsettings.json`, `appsettings.{Environment}.json`, and **environment
variables** with the `EmailSettings__Foo` form (double underscore = nested
property `EmailSettings.Foo`).

This means **you never edit `appsettings.json` for production secrets**.
Set env vars in the appropriate place per environment:

- **Docker dev**: `environment:` block in `docker/compose.idp.yml`.
- **VPS production**: `/etc/hmm-idp/idp.env`, loaded by the systemd unit's
  `EnvironmentFile=` directive.

---

## Test Docker env — already wired, no action required

`docker/compose.base-sqlite.yml` declares a `mailpit` service.
`docker/compose.idp.yml` points the IDP at it via the
`EmailSettings__*` env vars.

Bring the stack up:

```bash
cd ~/projects/hmm/docker
./test-env.sh                 # or: docker compose -f compose.base-sqlite.yml -f compose.idp.yml -f compose.api.yml up -d
```

Verify Mailpit is up:

```bash
curl -I http://localhost:8025
```

Register a test user (web or API), then refresh the Mailpit UI — the
verification email appears immediately. Click the embedded link, the
account flips to `EmailConfirmed = true`, and the user can sign in.

**Mailpit accepts every recipient address** — you can register with
`alice@example.com` or `bob@notarealdomain.test` and the captured email
shows up regardless. No DNS, no auth, no internet round-trip.

---

## VPS production — five operator steps

### 1. Create the mailbox at AspHostPortal

Sign in to the AspHostPortal control panel (Plesk- or cPanel-style),
navigate to **Mail → Email Accounts → Create**:

| Field | Value |
|---|---|
| Mailbox | `accounts@homemademessage.com` |
| Password | strong, unique — you won't log into webmail; it's only used for SMTP submission |
| Mailbox size | small (50–100 MB is enough; the IDP only sends, never reads) |

> Why `accounts@`? Industry-standard mailbox name for transactional mail.
> See `docs/` discussion on naming if curious.

### 2. Capture SMTP credentials from the panel

The panel typically shows a "Mail client config" or similar dialog. Note:

- **SMTP server** — usually `mail.homemademessage.com`. Sometimes
  AspHostPortal advertises a shared hostname like `smtp.asphostportal.com`.
  Use whatever the panel shows.
- **Port** — `587` with STARTTLS is the standard submission port. `465`
  with SMTPS works as a fallback.
- **Auth** — full email address (`accounts@homemademessage.com`) +
  the mailbox password from step 1.

### 3. DNS records (this is where most setups silently fail)

Add these three records at your DNS provider for `homemademessage.com`.
Without them, Gmail and Outlook will junk verification mail or reject it
outright. **All three are required for production.**

#### SPF — `TXT` on the apex

```
homemademessage.com.   TXT  "v=spf1 include:asphostportal.com -all"
```

The exact `include:` token is whatever AspHostPortal publishes in their
mail-config docs. Verify before saving.

#### DKIM — `TXT` on a selector

AspHostPortal's mail panel has a **DKIM** section that generates a key
pair. Enable it; copy the published TXT record they show (selector name,
e.g. `default._domainkey`, plus the `v=DKIM1; k=rsa; p=...` value) into
your DNS.

#### DMARC — `TXT` on `_dmarc`

Start permissive; you can tighten later.

```
_dmarc.homemademessage.com.   TXT  "v=DMARC1; p=none; rua=mailto:postmaster@homemademessage.com"
```

After a week of monitoring `rua` reports, change `p=none` →
`p=quarantine`, and eventually `p=reject`.

### 4. Wire the credentials on the VPS

`scripts/setup-idp-vps.sh` writes `/etc/hmm-idp/idp.env` with
placeholders. After step 1–3 above, edit the file:

```bash
ssh hmmidp                    # or: ssh -i ~/.ssh/20220830-2236.key ubuntu@132.145.102.175
sudo $EDITOR /etc/hmm-idp/idp.env
```

Replace the placeholder lines with:

```ini
EmailSettings__SmtpServer=mail.homemademessage.com
EmailSettings__SmtpPort=587
EmailSettings__UseSsl=true
EmailSettings__Username=accounts@homemademessage.com
EmailSettings__Password=<the mailbox password from step 1>
EmailSettings__SenderEmail=accounts@homemademessage.com
EmailSettings__SenderName=HomeMadeMessage
EmailSettings__ApplicationUrl=https://idp.homemademessage.com
```

Verify the file is mode 0640 owned by `root:hmm-idp` so the password isn't
world-readable:

```bash
sudo ls -l /etc/hmm-idp/idp.env
# -rw-r----- 1 root hmm-idp 642 Apr 28 14:00 /etc/hmm-idp/idp.env
```

Restart so the IDP picks up the new env:

```bash
sudo systemctl restart hmm-idp
sudo journalctl -u hmm-idp -n 30 --no-pager
```

### 5. Smoke test deliverability

From your laptop:

```bash
curl -X POST https://idp.homemademessage.com/Account/Register \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'Input.Username=smtp-test1' \
  --data-urlencode 'Input.Email=your-real-inbox@example.com' \
  --data-urlencode 'Input.Password=Test12345678!' \
  --data-urlencode 'Input.ConfirmPassword=Test12345678!' \
  -i | grep -E 'Location:|HTTP/'
```

Expected: `HTTP/2 302` with `Location:` pointing at
`/Account/RegisterConfirmation`. Within 30 s, check your inbox **and**
spam folder. If it lands in spam, run a deliverability check:

```bash
# https://www.mail-tester.com gives you a one-shot disposable address.
# Send a verification email to that address, then visit the URL it shows.
# Aim for 9/10 or better. Each missing point points at a concrete fix
# (typically: missing DKIM, weak SPF, no DMARC).
```

---

## Troubleshooting

| Symptom | Probable cause | Fix |
|---|---|---|
| Email never arrives, no error in logs | SMTP server / port / SSL flag wrong | Triple-check `EmailSettings__*` against AspHostPortal's panel; STARTTLS = `587 + UseSsl=true`; SMTPS = `465 + UseSsl=true` |
| `journalctl` shows `Connection timed out` | Outbound port blocked at the VPS | Try port `465` instead of `587`; if both blocked, switch to a transactional API provider (Resend / Postmark / Brevo all expose HTTPS APIs over 443) |
| `Authentication failed` / `535 5.7.8` | Wrong username or password | Username must be the **full** address `accounts@homemademessage.com`, not just `accounts` |
| Email arrives but lands in spam | Missing or wrong SPF / DKIM / DMARC | Run the domain through <https://www.mail-tester.com>; fix every red item |
| `journalctl` shows `Mail to be sent has not been authenticated` | DKIM signing not enabled in AspHostPortal | Enable DKIM in their mail panel; add the published selector record to DNS |
| Verification link in the email goes to `http://...` instead of `https://...` | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` not set, or Caddy isn't forwarding the headers | Confirm `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is in `/etc/hmm-idp/idp.env`, and that Caddy's `header_up X-Forwarded-Proto {scheme}` directive is present |
| Captured email's link returns 404 in dev | The link's host is `idp.homemademessage.com` (production) instead of `localhost:5001` | The IDP builds the URL from the *current* request — make sure you registered via `http://localhost:5001`, not by hitting the production URL |

---

## When AspHostPortal isn't enough

The AspHostPortal SMTP relay is fine for low-volume transactional mail
(verification, password resets, lockout notifications). It will start to
hurt when:

- Volume exceeds a few hundred emails per day (shared-IP rate limits).
- You need bounce / open / click tracking.
- A spike of registrations clogs the queue and verification emails arrive
  hours late.
- AspHostPortal's mail server's IP reputation drops and Gmail starts
  greylisting your sends.

When that happens, swap `EmailSettings__SmtpServer` to one of:

| Provider | Free tier | Notes |
|---|---|---|
| **Postmark** | 100/day forever | Best deliverability for transactional; SMTP and HTTPS API |
| **Resend** | 3000/month | Modern, developer-friendly, HTTPS API |
| **Brevo** (formerly Sendinblue) | 300/day | Decent free tier; both SMTP and API |
| **SendGrid** | 100/day | Largest, but more anti-abuse friction; SMTP and API |
| **AWS SES** | $0.10 per 1k after first 62k/month | Cheapest at volume; manual sandbox-removal step |

All of them speak SMTP, so the swap is config-only — nothing in the IDP
code changes. Just point `EmailSettings__SmtpServer` /
`EmailSettings__Username` / `EmailSettings__Password` at the new host.

---

## Where this is all defined

| Concern | File |
|---|---|
| `EmailSettings` POCO | `src/Hmm.Idp/Services/EmailService.cs` (bottom of file) |
| Env-var binding | ASP.NET Core default; no app code |
| Test Docker SMTP wiring | `docker/compose.base-sqlite.yml` (mailpit), `docker/compose.idp.yml` (env block) |
| VPS env-file template | `scripts/setup-idp-vps.sh` (heredoc inside step 6) |
| Email-send call sites | `Pages/Account/Register.cshtml.cs`, `Pages/Account/ResendConfirmation.cshtml.cs`, `Services/AccountLockoutService.cs`, `Services/PasswordResetService.cs` |
