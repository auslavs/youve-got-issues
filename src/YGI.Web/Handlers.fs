module Handlers
  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI
  open YGI.Dto
  open YGI.Events

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
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<IssueUpdateDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create "" projNum dto
          return! Api.UpdateIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }