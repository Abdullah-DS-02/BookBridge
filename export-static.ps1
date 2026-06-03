# export-static.ps1
# A script to export ASP.NET Core MVC frontend pages to static HTML files for Firebase Hosting

$LocalServer = "http://localhost:5000"
$DestDir = Join-Path $PSScriptRoot "public"

Write-Host "Creating output directory: $DestDir..."
if (Test-Path $DestDir) {
    Remove-Item -Recurse -Force $DestDir
}
New-Item -ItemType Directory -Force -Path $DestDir

# Copy wwwroot files to public
Write-Host "Copying assets from wwwroot to public..."
if (Test-Path "wwwroot") {
    Copy-Item -Path "wwwroot\*" -Destination $DestDir -Recurse -Force
}

$pages = @(
    @{ Route = "/"; Out = "index.html" },
    @{ Route = "/Home/About"; Out = "Home/About.html" },
    @{ Route = "/Home/Contact"; Out = "Home/Contact.html" },
    @{ Route = "/Home/FAQ"; Out = "Home/FAQ.html" },
    @{ Route = "/Home/Privacy"; Out = "Home/Privacy.html" },
    @{ Route = "/Home/Terms"; Out = "Home/Terms.html" },
    @{ Route = "/Books"; Out = "Books.html" },
    @{ Route = "/Books/Create"; Out = "Books/Create.html" },
    @{ Route = "/Account/Login"; Out = "Account/Login.html" },
    @{ Route = "/Account/Register"; Out = "Account/Register.html" }
)

foreach ($page in $pages) {
    $url = $LocalServer + $page.Route
    $outFile = Join-Path $DestDir $page.Out
    $outParent = Split-Path $outFile
    
    if (-not (Test-Path $outParent)) {
        New-Item -ItemType Directory -Force -Path $outParent
    }
    
    Write-Host "Downloading $url to $outFile..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $outFile -UseBasicParsing
        Write-Host "Successfully downloaded $($page.Route)"
    } catch {
        Write-Error "Failed to download $url : $_"
    }
}

Write-Host "Export complete!"
