$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = Get-Location
$watcher.IncludeSubdirectories = $false
$watcher.EnableRaisingEvents = $true

$changed = Register-ObjectEvent $watcher "Changed" -Action {

   [System.IO.FileInfo]$file = $eventArgs.FullPath

   if ($file.Extension -eq ".html")
   {
        write-host "Copying File: $($file.FullName)"
        $newlocation = "$($file.DirectoryName)\bin\Debug\netcoreapp3.1\$($file.Name)"
        write-host "Destination: $newlocation"
        copy-item $file.FullName -Destination $newlocation -Force

   }
}