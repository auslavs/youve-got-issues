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

  do Storage.initProjectSummary () |> ignore

  let loggingHandler: HttpHandler =
    fun next ctx -> task {
      let logger = ctx.GetLogger()
      use _ = logger.BeginScope(dict ["foo", "bar"])
      logger.LogInformation("Logging {Level}", "Information")
      return! Successful.OK "ok" next ctx
    }

  let app : HttpHandler =

    choose [
      GET  >=> route "/" >=> Index.getHandler
      POST >=> route "/" >=> Index.createNewProject
      GET  >=> routef "/%s" (fun proj -> Project.getHandler proj)
      POST >=> routef "/%s" (fun proj -> Project.createNewIssue proj)
      GET  >=> route "/demo" >=> htmlFile "index.html"
      GET  >=> route "/api/demo" >=> htmlFile "index.html"
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