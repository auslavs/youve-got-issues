module Handlers
  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI
  open YGI.Dto
  open YGI.Events

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