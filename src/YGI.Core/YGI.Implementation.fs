module internal YGI.Implementation

  open YGI.Common
  open YGI.Dto
  open YGI.Logging
  open System.Threading.Tasks

  type LogEvent<'TEvent> = Logger -> YgiEvent<'TEvent> -> unit -> 'TEvent
  type StoreState<'TState> = Logger -> 'TState -> Task<unit>
  type GetState<'TState> = Logger -> string -> Task<'TState>
  type UpdateState<'TState> = Logger -> 'TState -> string -> Task<unit>
  type LeaseCurrentState<'TState> = Logger -> string -> unit -> Task<'TState * string>
  type ApplyEvent<'TState,'TEvent> = 'TState -> 'TEvent -> Result<'TState,string>
  

  type LogNewProjectEvent = LogEvent<NewProjectDto>
  type StoreProjectState = StoreState<ProjectState>
  type UpdateProjectState = UpdateState<ProjectState>
  type CreateNewProject = NewProjectDto -> Result<ProjectState, string>
  type UpdateProjectSummary = Logger -> ProjectState -> TaskResult<unit, string>
  type AddNewProject = YgiEvent<NewProjectDto> -> TaskResult<ProjectState,string>


  let addNewProjectWorkflow
    (logger : Logger)
    (logEvent:LogNewProjectEvent)
    (createNewProject : CreateNewProject)
    (storeNewProject : StoreProjectState)
    (updateProjectSummary : UpdateProjectSummary)
    : AddNewProject =

    fun event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! newProject = createNewProject eventBody |> TaskResult.ofResult
        do! storeNewProject logger newProject |> TaskResult.ofTask
        do! updateProjectSummary logger newProject

        return newProject
      }


  type LogNewIssueEvent = LogEvent<NewIssueDto>
  type LeaseProjectState = LeaseCurrentState<ProjectStateDto>
  type AddIssueToProject = ApplyEvent<ProjectState,NewIssueDto>


  let addNewProjectIssueWorkflow
    (logger : Logger)
    (logEvent:LogNewIssueEvent)
    (leaseProjectState:LeaseProjectState)
    (addIssueToProject: AddIssueToProject)
    (updateProjectState:UpdateProjectState)
    (updateProjectSummary: UpdateProjectSummary)
    =

    fun projNum event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! dto, leaseId = leaseProjectState logger projNum () |> TaskResult.ofTask
        let! currentState = dto |> ProjectStateDto.toProjectState |> TaskResult.ofResult
        let! newState = addIssueToProject currentState eventBody |> TaskResult.ofResult
        do! updateProjectState logger newState leaseId |> TaskResult.ofTask
        do! updateProjectSummary logger newState

        return newState
      }

  type LogIssueUpdateEvent = LogEvent<IssueUpdateDto>
  type UpdateIssue = ApplyEvent<ProjectState,IssueUpdateDto>

  let updateIssueWorkflow
    (logger : Logger)
    (logEvent:LogIssueUpdateEvent)
    (leaseProjectState:LeaseProjectState)
    (updateIssue: UpdateIssue)
    (updateProjectState:UpdateProjectState)
    (updateProjectSummary: UpdateProjectSummary)
    =

    fun projNum event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! dto, leaseId = leaseProjectState logger projNum () |> TaskResult.ofTask
        let! currentState = dto |> ProjectStateDto.toProjectState |> TaskResult.ofResult
        let! newState = updateIssue currentState eventBody |> TaskResult.ofResult
        do! updateProjectState logger newState leaseId |> TaskResult.ofTask
        do! updateProjectSummary logger newState

        return newState
      }

  type GetProjectState = TaskResult<ProjectStateDto,string>
  type CreateIssueDetailViewDto = ProjectStateDto -> Result<IssueDetailViewDto,string>

  let getIssueDetailWorkFlow 
    (logger : Logger)
    (getProjectState:GetProjectState) 
    (createIssueDetail:CreateIssueDetailViewDto)
    =
    fun projNum issueNum ->
      taskResult {

        logger Info <| sprintf "Retrieving Project Issue,\r\nProject:%s, Issue No.%i" projNum issueNum

        let! dtoOpt = getProjectState |> TaskResult.ofTask

        let result = 
          match dtoOpt with
          | Result.Error _ -> 

            let logstr = 
              "Failed to retrieve the project state when creating the Issue Detail\r\n" +
              sprintf "Project:%s, Issue No.%i" projNum issueNum

            logger Warning <| logstr

            Result.Error logstr

          | Result.Ok dto -> dto |> createIssueDetail

        return! result |> TaskResult.ofResult
      }