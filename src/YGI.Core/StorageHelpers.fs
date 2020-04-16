module private YGI.StorageHelpers

  open Microsoft.WindowsAzure.Storage
  open Microsoft.WindowsAzure.Storage.Blob
  open Microsoft.WindowsAzure.Storage.Table
  open FSharp.Control.Tasks.V2
  open Microsoft.Extensions.Logging
  open System.Text.Json
  
  let private connString = System.Environment.GetEnvironmentVariable(Constants.StorageConnStr);

  let private storageAccount =
    let success, sa = CloudStorageAccount.TryParse(connString)
    match success with
    | false -> failwith "Failed to obtain a storage account key"
    | true -> sa

  let getTableReference tableName =
    task {
      let  tableClient = storageAccount.CreateCloudTableClient()
      let  table = tableClient.GetTableReference(tableName)
      let! _ = table.CreateIfNotExistsAsync()
      return table
    }
    
  let private getBlobReference containerId blobName =
    // Get the Blob Container
    let cloudBlobClient = storageAccount.CreateCloudBlobClient()         
    let cloudBlobContainer = cloudBlobClient.GetContainerReference(containerId)
    cloudBlobContainer.CreateIfNotExistsAsync() |> ignore

    // Get Blob Reference
    cloudBlobContainer.GetBlockBlobReference(blobName)

  let blobExists (blobRef:CloudBlockBlob) = 
    task {
      try
        return! blobRef.ExistsAsync()
      with
      | :? StorageException -> return false
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

  let WriteBlob containerId blobName text = 
    task {
        let cloudBlockBlob = getBlobReference containerId blobName
        // Write Blob
        return! cloudBlockBlob.UploadTextAsync(text)
    }

  let CreateBlob containerId blobName text = 
    task {
      let cloudBlockBlob = getBlobReference containerId blobName
      let! exists = blobExists cloudBlockBlob

      match exists with
      | false -> return! cloudBlockBlob.UploadTextAsync(text)
      | true -> failwithf "Blob already exists: %s" text
    }

  let retrieveDto<'T> (logger:ILogger) container blobName  = 
    task {
        let! result = GetBlob container blobName

        let dto = 
          match result with
          | Some str -> Some (JsonSerializer.Deserialize<'T>(str))
          | None ->
            logger.LogWarning <| sprintf "Could not find blob :%s" blobName 
            None

        return dto
    }

  let StoreDto (logger:ILogger) container dtoName blobName dto = 
    task {
        try
          let stateJson = JsonSerializer.Serialize(dto)
          let! result = WriteBlob container blobName stateJson
          return result
        with 
        | ex -> 
          sprintf "Failed to serialise and store Dto %s\r\n%A\r\nException:\r\n%A" dtoName dto ex
          |> logger.LogError
          raise ex
    }

  let StoreNewDto (logger:ILogger) container dtoName blobName dto = 
    task {
        try
          let stateJson = JsonSerializer.Serialize(dto)
          let! result = CreateBlob container blobName stateJson
          return result
        with 
        | ex -> 
          sprintf "Failed to serialise and store new Dto %s\r\n%A\r\nException:\r\n%A" dtoName dto ex
          |> logger.LogError
          raise ex
    }

  let StoreEvent (table:CloudTable) ctor (logger:ILogger) evt () =
    task {
      return! 
        evt
        |> ctor
        |> TableOperation.Insert 
        |> table.ExecuteAsync
    }