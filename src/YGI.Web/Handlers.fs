module Handlers
  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI
  open YGI.Dto
  open YGI.Events
  open System
  open System.Threading
  open System.Net.Http
  open System.IO
  open Microsoft.AspNetCore.Http.Features

  let GetRequestId (ctx : HttpContext) = 
    let result,cid = ctx.Items.TryGetValue "MS_AzureFunctionsRequestID"
    match result with
    | false -> failwith "Failed to get request id"
    | true -> cid.ToString()

  let getProjectList =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let! summary = Storage.getProjectSummary ()
        return! (Successful.OK summary) next ctx 
      }

  let getProjectIssuesList proj =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
          let logger = Logging.log <| ctx.GetLogger()
          let! summary = Storage.getProject logger proj ()

          match summary with 
          | Some s -> 
            return! (Successful.OK s) next ctx 
          | None -> 
            let response = RequestErrors.NOT_FOUND "Page not found"
            return! response next ctx            
        }

  let createNewProject =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<NewProjectDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid dto.ProjectNumber dto
          return! Api.CreateNewProject logger event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let createNewIssue projNum : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<NewIssueDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid projNum dto
          return! Api.CreateNewIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let getIssueDetail proj id : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()

        let! taskResult = taskResult {
          return! Api.GetIssueModel logger proj id
        }

        let response =
          match taskResult with
          | Ok issueDetail -> (Successful.OK issueDetail) next ctx
          | Error err -> (RequestErrors.NOT_FOUND err) next ctx

        return! response           
      }

  let updateIssue projNum _ : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<IssueUpdateDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create cid projNum dto
          return! Api.UpdateIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }

  let uploadAttachment projNum issueItemNo : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let cid = GetRequestId ctx
        let logger = Logging.log <| ctx.GetLogger()

        let uploadAttachment = YGI.Storage.uploadAttachment logger
        let createEvent      = YgiEvent.create cid projNum

        let toFileStream seq (file:IFormFile) = 
          let stream = new MemoryStream()
          file.CopyTo(stream)
          {
            Seq           = seq
            Id            = Guid.NewGuid().ToString()
            ProjectNumber = projNum
            IssueItemNo   = issueItemNo
            Filename      = file.FileName
            ContentType   = file.ContentType
            Stream        = stream
          } 

        /// Convert to domain friendly Attachemnt Stream
        let files = ctx.Request.Form.Files |> Seq.mapi (fun i f -> toFileStream i f)

        /// Upload each attachment
        let! uploadDetails = 
          files
          |> Seq.map uploadAttachment
          |> Threading.Tasks.Task.WhenAll

        let uploadEvent = createEvent uploadDetails
        let! result = Api.AddAttachments logger projNum uploadEvent

        let response =
          match result with
          | Result.Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
       }
