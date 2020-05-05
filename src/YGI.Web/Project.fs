namespace YGI

module Project =

  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open YGI.Dto
  open YGI.Events

  let getApiHandler proj =
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

  let createNewIssue projNum : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<NewIssueDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create "" projNum dto
          return! Api.CreateNewIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }
  