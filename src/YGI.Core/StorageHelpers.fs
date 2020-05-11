module private YGI.StorageHelpers

  open YGI.Logging
  open Microsoft.Azure.Storage.Blob
  open Microsoft.Azure.Cosmos.Table
  open FSharp.Control.Tasks.V2.ContextSensitive
  open System
  open Microsoft.Azure.Storage
  open System.Text
  
  let private connString = System.Environment.GetEnvironmentVariable(Constants.StorageConnStr);

  let private tableAccount =
    let success, sa = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.TryParse(connString)
    match success with
    | false -> failwith "Failed to obtain a storage account key"
    | true -> sa

  let private blobAccount =
    let success, sa = Microsoft.Azure.Storage.CloudStorageAccount.TryParse(connString)
    match success with
    | false -> failwith "Failed to obtain a storage account key"
    | true -> sa

  let getTableReference tableName =
    task {
      let  tableClient = tableAccount.CreateCloudTableClient()
      let  table = tableClient.GetTableReference(tableName)
      let! _ = table.CreateIfNotExistsAsync()
      return table
    }

  let private getBlobLease (blob:CloudBlockBlob) =
    task {
      let leaseTime = TimeSpan.FromSeconds 15.0 |> Nullable
      return! blob.AcquireLeaseAsync(leaseTime)
    }
    
  let private getBlobReference containerId blobName =
    // Get the Blob Container
    let cloudBlobClient = blobAccount.CreateCloudBlobClient()         
    let cloudBlobContainer = cloudBlobClient.GetContainerReference(containerId)
    cloudBlobContainer.CreateIfNotExistsAsync() |> ignore

    // Get Blob Reference
    cloudBlobContainer.GetBlockBlobReference(blobName)

  let blobExists (blobRef:CloudBlockBlob) = 
    task {
      try
        return! blobRef.ExistsAsync()
      with ex -> 
        printfn "blob does not esist %A" blobRef.Name
        return false
    }

  let GetBlob containerId blobName = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName

      let! exists = blobExists cloudBlockBlob

      match exists with
      | true ->
        // Read Blob
        let! file = cloudBlockBlob.DownloadTextAsync()
        return Some file
      | false -> return None
      
    }

  let GetBlobWithLease containerId blobName = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName

      let! exists = blobExists cloudBlockBlob

      match exists with
      | true ->

        // Lease Blob
        let! lease = getBlobLease cloudBlockBlob

        // Read Blob
        let! file = cloudBlockBlob.DownloadTextAsync()

        return Some (file, lease)
      | false -> return None
      
    }

  let CreateBlob containerId blobName text = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName
      let! exists = blobExists cloudBlockBlob

      match exists with
      | false -> return! cloudBlockBlob.UploadTextAsync(text)
      | true -> failwithf "Blob already exists: %s" blobName
    }


  let UploadBlobFromStream containerId blobName contentType stream = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName
      let! exists = blobExists cloudBlockBlob

      match exists with
      | false ->
        cloudBlockBlob.Properties.ContentType <- contentType
        do! cloudBlockBlob.UploadFromStreamAsync(stream)
        do! stream.DisposeAsync()
        return ()
      | true -> failwithf "Blob already exists: %s" blobName
    }

  let UpdateBlob containerId blobName leaseId text = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName
      let! exists = blobExists cloudBlockBlob

      match exists with
      | true -> 
        let lease = AccessCondition.GenerateLeaseCondition(leaseId)
        do! cloudBlockBlob.UploadTextAsync(text,Encoding.UTF8,lease,null,null)
        do! cloudBlockBlob.ReleaseLeaseAsync(lease)
      | false -> failwithf "Blob does not exist: %s - %s" blobName text
    }

  let StoreEvent (table:CloudTable) ctor (logger:Logger) evt () =
    task {
      return! 
        evt
        |> ctor
        |> TableOperation.Insert 
        |> table.ExecuteAsync
    }