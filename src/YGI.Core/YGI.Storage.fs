module YGI.Storage

  open YGI
  open YGI.Common
  open StorageHelpers
  open Microsoft.WindowsAzure.Storage.Table
  open FSharp.Control.Tasks.V2
  open Microsoft.Extensions.Logging
  open Microsoft.Extensions.Logging.Abstractions
  open Newtonsoft.Json
  open System
  open YGI.Dto
  
  let private eventsTable = (getTableReference Constants.EventTable).Result

  let getProjectSummary (logger:ILogger) () = 
    task {
      let! summaryOpt = StorageHelpers.GetBlob Constants.ContainerId Constants.Summary
    
      let summary = 
        match summaryOpt with
        | Some str -> 

          let errHandler = 
            new EventHandler<Serialization.ErrorEventArgs>
              ( fun obj args -> 
                logger.LogError <| sprintf "Object: %A - Args: %A" obj args )

          let settings = new JsonSerializerSettings()
          settings.MissingMemberHandling <- MissingMemberHandling.Ignore
          settings.ObjectCreationHandling <- ObjectCreationHandling.Replace
          settings.Error <- errHandler
          JsonConvert.DeserializeObject<SummaryDto>(str,settings)
        | None -> failwith "Failed to retrieve the Project Summary list" 

      return summary
    }

  let initProjectSummary () = 
    
      task {
        let! summaryOpt = StorageHelpers.GetBlob Constants.ContainerId Constants.Summary

        match summaryOpt with
        | Some _ -> ()
        | None -> 
            let nullLogger = new Logger<NullLogger>(new NullLoggerFactory())
            let newSummary = Summary.init |> SummaryDto.toSummaryDto

            do! StorageHelpers.StoreDto nullLogger Constants.ContainerId "ProjectSummaryDto" Constants.Summary newSummary
        return ()
      }

  let getProject projNum () =
    task {
      let blob = Constants.projectBlobPath projNum
      let! summaryOpt = StorageHelpers.GetBlob Constants.ContainerId blob
      return summaryOpt |> Option.map (fun s -> JsonConvert.DeserializeObject<ProjectStateDto>(s))
    }



  

