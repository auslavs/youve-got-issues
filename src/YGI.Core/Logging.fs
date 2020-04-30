module YGI.Logging

  open Microsoft.Extensions.Logging

  type LogType =
  | Info
  | Warning
  | Error
  | Critical

  type Logger = LogType -> string -> unit

  let log (logger:ILogger) : Logger =
    fun logType str -> 
      match logType with
      | Info -> logger.LogInformation str
      | Warning -> logger.LogWarning str
      | Error -> logger.LogError str
      | Critical -> logger.LogCritical str
