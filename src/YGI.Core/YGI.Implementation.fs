module internal YGI.Implementation

  open Microsoft.Extensions.Logging
  open YGI.Common
  open YGI.Dto
  open System.Threading.Tasks

  type LogEvent<'TEvent> = ILogger -> YgiEvent<'TEvent> -> unit -> 'TEvent
  type StoreState<'TState> = ILogger -> 'TState -> Task<unit>
  type LoadCurrentState<'TState> = ILogger -> string -> unit -> Task<'TState>
  type ApplyEvent<'TState,'TEvent> = 'TState -> 'TEvent -> Result<'TState,string>
  

  type LogNewProjectEvent = LogEvent<NewProjectDto>
  type StoreProjectState = StoreState<ProjectState>
  type CreateNewProject = NewProjectDto -> Result<ProjectState, string>
  type UpdateProjectSummary = ILogger -> ProjectState -> TaskResult<unit, string>
  type AddNewProject = YgiEvent<NewProjectDto> -> TaskResult<ProjectState,string>

  //type LoadCurrentState<'TState> = unit -> 'TState
  //type ApplyEvent<'TEvent,'TState> = 'TState -> 'TEvent -> Result<'TState, string>
  //type StoreState<'TState> = 'TState -> unit -> Task<unit>

  let addNewProjectWorkflow
    (logger : ILogger)
    (logEvent:LogNewProjectEvent)
    (createNewProject : CreateNewProject)
    (storeNewProject : StoreProjectState)
    (updateProjectSummary : UpdateProjectSummary)
    : AddNewProject =

    fun event -> 
      taskResult {

        logger.LogInformation <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! newProject = createNewProject eventBody |> TaskResult.ofResult
        do! storeNewProject logger newProject |> TaskResult.ofTask
        do! updateProjectSummary logger newProject

        return newProject
      }


  type LogNewIssueEvent = LogEvent<NewIssueDto>
  type GetProjectState = ILogger -> string -> Task<unit>
  type LoadProjectState = LoadCurrentState<ProjectStateDto>
  type AddIssueToProject = ApplyEvent<ProjectState,NewIssueDto>


  let addNewProjectIssueWorkflow
    (logger : ILogger)
    (logEvent:LogNewIssueEvent)
    (loadProjectState:LoadProjectState)
    (addIssueToProject: AddIssueToProject)
    (storeNewState:StoreProjectState)
    (updateProjectSummary: UpdateProjectSummary)
    =

    fun projNum event -> 
      taskResult {

        logger.LogInformation <| sprintf "Recieved Event:\r\n%A" event

        let  eventBody = logEvent logger event ()
        let! currentStateDto = loadProjectState logger projNum ()|> TaskResult.ofTask
        let! currentState = currentStateDto |> ProjectStateDto.toProjectState |> TaskResult.ofResult
        let! newState = addIssueToProject currentState eventBody |> TaskResult.ofResult
        do!  storeNewState logger newState |> TaskResult.ofTask
        do! updateProjectSummary logger newState

        return newState
      }