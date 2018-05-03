[CmdletBinding()]
Param(
    [Parameter(Mandatory=$True)]
    [string]$RepoPath,
    
    [Parameter(Mandatory=$True)]
    [string]$RepoUri,

    [Parameter(Mandatory=$True)]
    [string]$Commit
)

if (Test-Path "$RepoPath") {
    Remove-Item "$RepoPath" -Recurse -Force
}

git clone "$RepoUri" "$RepoPath" -q
Copy-Item -Path _site/* -Destination "$RepoPath" -Recurse

pushd "$RepoPath"

git config user.name "Pavel Fedarovich"
git config user.email "p.fedarovich@gmail.com"
git config core.autocrlf false
git add -A
git commit -m "Updated documentation to commit $Commit (https://github.com/fedarovich/qbittorrent-net-client/commit/$Commit)"
git push --quiet

popd
