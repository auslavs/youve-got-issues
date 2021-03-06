namespace YGI


module App =

  open System
  open Giraffe
  open Microsoft.AspNetCore.Http
  open Microsoft.Azure.WebJobs
  open Microsoft.Azure.WebJobs.Extensions.Http
  open System.Threading.Tasks
  open FSharp.Control.Tasks.V2
  open Microsoft.Extensions.Logging

  do Storage.initProjectSummary () |> Task.FromResult |> ignore

  let loggingHandler: HttpHandler =
    fun next ctx -> task {
      let logger = ctx.GetLogger()
      use _ = logger.BeginScope(dict ["foo", "bar"])
      logger.LogInformation("Logging {Level}", "Information")
      return! Successful.OK "ok" next ctx
    }

  let notLoggedIn = RequestErrors.UNAUTHORIZED "" "" "You must be logged in."

  let mustBeLoggedIn = requiresAuthentication notLoggedIn

  //let indexPage   : HttpHandler = route "/" >=> htmlFile "index.html"
  //let projectPage : HttpHandler = routex "\/P\d{5}" >=> htmlFile "project.html"
  //let issuesPage  : HttpHandler = routex "\/P\d{5}\/[0-9]+" >=> htmlFile "issue.html"
  //let issuesApi   : HttpHandler = routex "\/api\/(P\d{5})\/([0-9]+)" 

  let app : HttpHandler =
    mustBeLoggedIn >=>
      choose [
        GET  >=> route "/api/summary" >=> Handlers.getProjectList
        GET  >=> route "/api/claims" >=> Handlers.getClaims
        GET  >=> route "/api/user" >=> Handlers.getUser
        GET  >=> routef "/api/P%s/export" (fun proj -> Handlers.exportProject ("P" + proj))
        GET  >=> routef "/api/P%s" (fun proj -> Handlers.getProjectIssuesList ("P" + proj))
        GET  >=> routef "/api/P%s/%i" (fun (proj,id) -> Handlers.getIssueDetail ("P" + proj) id)
        POST >=> routef "/api/P%s" (fun _ -> Handlers.createNewProject)
        POST >=> routef "/api/P%s/issues" (fun proj -> Handlers.createNewIssue ("P" + proj))
        POST >=> routef "/api/P%s/%i/addComment" (fun (proj,id) -> Handlers.addComment ("P" + proj) id)
        PUT  >=> routef "/api/P%s/%i" (fun (proj,id) -> Handlers.updateIssue ("P" + proj) id)
        PUT  >=> routef "/api/P%s/%i/upload" (fun (proj,id) -> Handlers.uploadAttachment ("P" + proj) id)
      ]

  let errorHandler (ex : exn) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse
    >=> ServerErrors.INTERNAL_ERROR ex.Message

  [<FunctionName "Giraffe">]
  let run ([<HttpTrigger (AuthorizationLevel.Anonymous, Route = "{*any}")>] req : HttpRequest, context : ExecutionContext, log : ILogger) =
    let hostingEnvironment = req.HttpContext.GetHostingEnvironment()
    hostingEnvironment.ContentRootPath <- context.FunctionAppDirectory
    let func = Some >> Task.FromResult
    { new Microsoft.AspNetCore.Mvc.IActionResult with
        member _.ExecuteResultAsync(ctx) = 
          task {
            try
              return! app func ctx.HttpContext :> Task
            with exn ->
              return! errorHandler exn log func ctx.HttpContext :> Task
          }
          :> Task }