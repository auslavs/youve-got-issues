module YGI.Storage

  open YGI
  open YGI.Logging
  open YGI.Common
  open FSharp.Control.Tasks.V2.ContextSensitive
  open Newtonsoft.Json
  open YGI.Dto
  open System.IO
  
  //let private eventsTable = (getTableReference Constants.EventTable).Result
  let private getBlob = StorageHelpers.GetBlob Constants.ContainerId
  let private createBlob = StorageHelpers.CreateBlob Constants.ContainerId
  let private leaseBlob = StorageHelpers.GetBlobWithLease Constants.ContainerId
  let private updateBlob = StorageHelpers.UpdateBlob Constants.ContainerId
  let private uploadBlob = StorageHelpers.UploadBlobFromStream Constants.ContainerId

  let initProjectSummary () = 
    
      task {
        let! summaryOpt = getBlob Constants.Summary

        match summaryOpt with
        | Some _ -> ()
        | None -> 
            let newSummary = 
              Summary.init 
              |> SummaryDto.toSummaryDto
              |> JsonConvert.SerializeObject
            do! createBlob Constants.Summary newSummary
        return ()
      }

  let getProjectSummary () = 
    task {
      let! summaryOpt = getBlob Constants.Summary
    
      let summary = 
        match summaryOpt with
        | Some str -> 
          JsonConvert.DeserializeObject<SummaryDto>(str)
        | None -> failwith "Failed to retrieve the Project Summary list" 

      return summary
    }

  let leaseProjectSummary (logger:Logger) () = 
    task {

      logger Info <| sprintf "Leasing the Project Summary"

      let! summaryOpt = leaseBlob Constants.Summary
    
      let summary = 
        match summaryOpt with
        | Some (str,leaseId) -> 
            let dto = JsonConvert.DeserializeObject<SummaryDto>(str)
            (dto,leaseId)
        | None -> failwith "Failed to retrieve and lease the Project Summary list" 

      return summary
    }

  let updateProjectSummary (logger:Logger) (summary:SummaryDto) leaseId () =
    task {
      logger Info <| sprintf "Updating Project Summary: %A" summary

      let jsonStr = summary |> JsonConvert.SerializeObject
      do! updateBlob Constants.Summary leaseId jsonStr
      return ()
    }

  let storeNewProject (logger:Logger) (newProject:ProjectStateDto) () =
    task {
      logger Info <| sprintf "Storing New Project: %A" newProject

      let blob = Constants.projectBlobPath newProject.ProjectNumber
      let jsonStr = newProject |> JsonConvert.SerializeObject
      do! createBlob blob jsonStr
      return ()
    }

  let getProject (logger:Logger) projNum () =
    task {

      logger Info <| sprintf "Retrieving Project: %s" projNum

      let blob = Constants.projectBlobPath projNum
      let! blobOpt = getBlob blob

      match blobOpt with
      | Some str -> 
          let dto = JsonConvert.DeserializeObject<ProjectStateDto>(str)
          return Some dto
      | None -> 
          logger Warning <| sprintf "Failed to retrieve the Project State: %s" projNum
          return None
    }

  let leaseProject (logger:Logger) projNum () =
    task {

      logger Info <| sprintf "Leasing Project: %s" projNum

      let blob = Constants.projectBlobPath projNum
      let! blobOpt = leaseBlob blob

      match blobOpt with
      | Some (str,leaseId) -> 
          let dto = JsonConvert.DeserializeObject<ProjectStateDto>(str)
          return (dto,leaseId)
      | None -> 
          return failwith "Failed to retrieve and lease the Project" 
    }

  let updateProject (logger:Logger) (project:ProjectStateDto) leaseId () =
    task {
      logger Info <| sprintf "Updating Project: %A" project

      let blob = Constants.projectBlobPath project.ProjectNumber
      let jsonStr = project |> JsonConvert.SerializeObject
      return! updateBlob blob leaseId jsonStr
    }

  let uploadAttachment (logger:Logger) (file:AttachmentStream) =
    task{
      logger Info <| sprintf "Uploading Attachement: %s - %s" file.Id file.Filename
      let blobPath = Constants.projectAttachmentsPath file.ProjectNumber file.IssueItemNo file.Id
      do! uploadBlob blobPath file.ContentType file.Stream

      let blobDetails : AttachmentDetailsDto = {
        Id            = file.Id
        ProjectNumber = file.ProjectNumber
        IssueItemNo   = file.IssueItemNo
        Filename      = file.Filename
        ContentType   = file.ContentType
        RelativeUrl   = blobPath
      }

      return blobDetails
    }







  

