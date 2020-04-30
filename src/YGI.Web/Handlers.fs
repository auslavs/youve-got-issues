module Handlers
  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI

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