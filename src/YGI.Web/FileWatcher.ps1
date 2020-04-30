$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = Get-Location
$watcher.IncludeSubdirectories = $true
$watcher.EnableRaisingEvents = $true

function Store-Blob
{
    param(
        [Parameter(ValueFromPipeline)]
        [string] $sourceFilePath,
        [string] $destination,
        [string] $contentType
    )
  
    write-host "Copying File to local blob emulator: $sourceFilePath"
    $ctx = New-AzStorageContext -Local
    $container = $ctx | Get-AzStorageContainer -Name site

    Set-AzStorageBlobContent -File $sourceFilePath `
        -Container $container.Name `
        -Blob $destination `
        -Context $ctx `
        -Properties @{"ContentType" = $contentType;} `
        -Force

}

$changed = Register-ObjectEvent $watcher "Changed" -Action {

   [System.IO.FileInfo]$file = $eventArgs.FullPath
   [string]$filepath = $eventArgs.FullPath
   [string]$folder = $watcher.Path
   [string]$blobpath = $filepath.Replace("$folder\","")


    switch($file.Extension) {

        ".html" { 
            Store-Blob `
            -sourceFilePath $filepath `
            -destination $blobpath `
            -contentType "text/html; charset=utf-8"
        }

        ".css" { 
            Store-Blob `
            -sourceFilePath $filepath `
            -destination $blobpath `
            -contentType "text/css; charset=utf-8"
        }
        ".js" { 
            Store-Blob `
            -sourceFilePath $filepath `
            -destination $blobpath `
            -contentType "text/javascript; charset=utf-8"
        }

    }

}