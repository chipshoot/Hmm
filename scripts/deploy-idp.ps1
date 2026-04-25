<#
.SYNOPSIS
  Publish Hmm.Idp locally and push it to the Oracle VPS as a native
  systemd service. Windows counterpart of deploy-idp.sh.

.DESCRIPTION
  Uses the OpenSSH client built into Windows 10/11 (ssh.exe / scp.exe)
  to upload the dotnet publish output to the VPS, then triggers a
  service restart over SSH. No rsync required.

.PARAMETER Action
  deploy   - publish + upload + restart (default if -Action omitted with -Deploy)
  publish  - dotnet publish only, no upload
  restart  - restart hmm-idp service on VPS
  status   - remote systemctl status
  logs     - follow remote journalctl
  ssh      - open interactive SSH session

.PARAMETER NoPublish
  Skip the dotnet publish step (use existing publish output).

.PARAMETER VpsHost
  Override default VPS hostname or IP. Default: 132.145.102.175

.PARAMETER VpsUser
  Override default SSH user. Default: ubuntu

.PARAMETER SshKey
  Path to SSH private key. Default: ~/.ssh/20220830-2236.key

.EXAMPLE
  .\deploy-idp.ps1 -Deploy
.EXAMPLE
  .\deploy-idp.ps1 -Deploy -NoPublish
.EXAMPLE
  .\deploy-idp.ps1 -Logs
#>
[CmdletBinding(DefaultParameterSetName = 'Deploy')]
param(
    [Parameter(ParameterSetName = 'Deploy')]  [switch] $Deploy,
    [Parameter(ParameterSetName = 'Publish')] [switch] $Publish,
    [Parameter(ParameterSetName = 'Restart')] [switch] $Restart,
    [Parameter(ParameterSetName = 'Status')]  [switch] $Status,
    [Parameter(ParameterSetName = 'Logs')]    [switch] $Logs,
    [Parameter(ParameterSetName = 'Ssh')]     [switch] $Ssh,

    [switch] $NoPublish,

    [string] $VpsHost   = $(if ($env:IDP_VPS_HOST)   { $env:IDP_VPS_HOST }   else { '132.145.102.175' }),
    [string] $VpsUser   = $(if ($env:IDP_VPS_USER)   { $env:IDP_VPS_USER }   else { 'ubuntu' }),
    [string] $SshKey    = $(if ($env:IDP_SSH_KEY)    { $env:IDP_SSH_KEY }    else { Join-Path $HOME '.ssh/20220830-2236.key' }),
    [string] $RemoteDir = $(if ($env:IDP_REMOTE_DIR) { $env:IDP_REMOTE_DIR } else { '/opt/hmm-idp' }),
    [string] $RemoteStage = '/tmp/hmm-idp-staging',
    [string] $AppUser   = 'hmm-idp',
    [string] $Service   = 'hmm-idp',
    [string] $SrcDir    = $(Join-Path $PSScriptRoot '..\src\Hmm.Idp'),
    [string] $PublishOut = $(Join-Path $env:TEMP 'hmm-idp-publish')
)

$ErrorActionPreference = 'Stop'

function Banner($text) {
    Write-Host ''
    Write-Host '============================================================' -ForegroundColor Cyan
    Write-Host $text                                                          -ForegroundColor Cyan
    Write-Host '============================================================' -ForegroundColor Cyan
}

function Ensure-LocalTools {
    foreach ($cmd in 'dotnet','ssh','scp') {
        if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
            throw "Required command '$cmd' not found on PATH. Install OpenSSH client (Settings -> Apps -> Optional features) and the .NET SDK."
        }
    }
    if (-not (Test-Path -LiteralPath $SshKey)) {
        throw "SSH key not found at: $SshKey"
    }
}

function Ssh-Args {
    @('-i', $SshKey, '-o', 'StrictHostKeyChecking=accept-new')
}

function Ensure-RemoteReachable {
    $args = (Ssh-Args) + @('-o','ConnectTimeout=10', "$VpsUser@$VpsHost", 'true')
    & ssh @args 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot SSH to $VpsUser@$VpsHost with $SshKey (ssh-add the key if it has a passphrase)."
    }
}

function Publish-Local {
    Banner 'Publishing Hmm.Idp (Release, framework-dependent)'
    Write-Host "  source: $SrcDir"
    Write-Host "  output: $PublishOut"
    if (Test-Path -LiteralPath $PublishOut) {
        Remove-Item -LiteralPath $PublishOut -Recurse -Force
    }
    New-Item -ItemType Directory -Path $PublishOut | Out-Null
    Push-Location $SrcDir
    try {
        # No explicit .csproj — dotnet finds the single project in $SrcDir.
        # Keeps the script template-clean so deploy-api.ps1 is a near-pure copy.
        & dotnet publish -c Release -o $PublishOut --nologo
        if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
    } finally {
        Pop-Location
    }
    $count = (Get-ChildItem -LiteralPath $PublishOut -Recurse -File).Count
    $size  = (Get-ChildItem -LiteralPath $PublishOut -Recurse -File | Measure-Object -Property Length -Sum).Sum
    Write-Host ("  {0} files, {1:N1} MB" -f $count, ($size / 1MB))
}

function Upload-And-Install {
    Banner "Uploading to $VpsUser@${VpsHost}:$RemoteDir"

    # Pre-clean staging on the VPS so removed files locally are also removed remotely.
    & ssh @(Ssh-Args) "$VpsUser@$VpsHost" "rm -rf '$RemoteStage' && mkdir -p '$RemoteStage'"
    if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare remote staging dir' }

    # scp the publish output. -r recursive, -p preserves modification times.
    # Note: we exclude *.pdb by listing files explicitly.
    $files = Get-ChildItem -LiteralPath $PublishOut -Recurse -File `
        | Where-Object { $_.Extension -ne '.pdb' -and $_.Name -ne 'appsettings.Development.json' }

    # Most efficient: scp the directory tree in one shot, then delete .pdb files server-side.
    & scp @(Ssh-Args) -r "$PublishOut/*" "${VpsUser}@${VpsHost}:$RemoteStage/"
    if ($LASTEXITCODE -ne 0) { throw 'scp upload failed' }

    & ssh @(Ssh-Args) "$VpsUser@$VpsHost" @"
set -e
find '$RemoteStage' -name '*.pdb' -delete 2>/dev/null || true
rm -f '$RemoteStage/appsettings.Development.json' 2>/dev/null || true
"@

    Banner "Atomic swap into $RemoteDir + restart $Service"
    & ssh @(Ssh-Args) "$VpsUser@$VpsHost" @"
set -e
sudo systemctl stop $Service 2>/dev/null || true
sudo rsync -a --delete '$RemoteStage/' '$RemoteDir/'
sudo chown -R ${AppUser}:${AppUser} '$RemoteDir'
sudo systemctl enable $Service
sudo systemctl start $Service
sleep 2
sudo systemctl is-active $Service
"@
    if ($LASTEXITCODE -ne 0) { throw 'Remote install/restart failed' }
}

# Resolve the action.
$action = $PSCmdlet.ParameterSetName.ToLower()
if ($action -eq 'deploy' -and -not $Deploy) {
    Write-Host "Usage: .\deploy-idp.ps1 -Deploy [-NoPublish]   |   -Publish | -Restart | -Status | -Logs | -Ssh"
    exit 1
}

switch ($action) {
    'publish' {
        Ensure-LocalTools
        Publish-Local
    }
    'deploy' {
        Ensure-LocalTools
        Ensure-RemoteReachable
        if (-not $NoPublish) { Publish-Local }
        if (-not (Test-Path -LiteralPath $PublishOut)) {
            throw "Publish output missing at $PublishOut — run -Publish first."
        }
        Upload-And-Install
        Banner 'Done'
        Write-Host "  Tail logs:    .\deploy-idp.ps1 -Logs"
        Write-Host "  Verify OIDC:  curl https://idp.homemademessage.com/.well-known/openid-configuration"
    }
    'restart' {
        Ensure-RemoteReachable
        & ssh @(Ssh-Args) "$VpsUser@$VpsHost" "sudo systemctl restart $Service && sudo systemctl is-active $Service"
    }
    'status' {
        Ensure-RemoteReachable
        & ssh @(Ssh-Args) "$VpsUser@$VpsHost" "sudo systemctl status $Service --no-pager -l | head -40"
    }
    'logs' {
        Ensure-RemoteReachable
        & ssh @(Ssh-Args) -t "$VpsUser@$VpsHost" "sudo journalctl -u $Service -f --no-pager"
    }
    'ssh' {
        & ssh @(Ssh-Args) -t "$VpsUser@$VpsHost"
    }
}
