module internal YGI.Implementation

  open YGI.Common
  open YGI.Dto
  open YGI.Logging
  open YGI.InternalTypes

  // Workflow Types
   
  type AddNewProjectWflow = 
    CreateNewProject 
      -> StoreProjectState
      -> UpdateProjectSummary
      -> YgiEvent<NewProjectDto> 
      -> TaskResult<ProjectState,string>

  type AddNewIssuetoProjectWflow = 
    LeaseProjectState 
      -> AddIssueToProject
      -> UpdateProjectState
      -> UpdateProjectSummary
      -> string         // Project Number
      -> YgiEvent<NewIssueDto> 
      -> TaskResult<ProjectState,string>

  type UpdateIssueWflow =
    string         // Project Number
      -> LeaseProjectState
      -> UpdateIssue
      -> UpdateProjectState
      -> UpdateProjectSummary
      -> YgiEvent<IssueUpdateDto>
      -> TaskResult<ProjectState,string>

  type AddCommentWflow =
    string         // Project Number
      -> LeaseProjectState
      -> AddNewComment
      -> UpdateProjectState
      -> UpdateProjectSummary
      -> YgiEvent<NewCommentDto>
      -> TaskResult<ProjectState,string>

  type AddAttchmentWflow =
    string         // Project Number
      -> LeaseProjectState
      -> AddAttachement
      -> UpdateProjectState
      -> UpdateProjectSummary
      -> YgiEvent<AttachmentDetailsDto array>
      -> TaskResult<ProjectState,string>

  // Workflow Implementation

  let addNewProjectWorkflow
    (logger : Logger)
    (logEvent:LogNewProjectEvent)
    : AddNewProjectWflow =

    fun createNewProject storeNewProject updateProjectSummary event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! newProject = createNewProject eventBody |> TaskResult.ofResult
        do! storeNewProject logger newProject |> TaskResult.ofTask
        do! updateProjectSummary logger newProject

        return newProject
      }

  let addNewIssuetoProjectWorkflow
    (logger : Logger)
    (logEvent:LogNewIssueEvent)
    : AddNewIssuetoProjectWflow =

    fun leaseProjectState addIssueToProject updateProjectState updateProjectSummary projNum event -> 
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

  let updateIssueWorkflow
    (logger : Logger)
    (logEvent:LogIssueUpdateEvent)
    : UpdateIssueWflow =

    fun projNum leaseProjectState updateIssue updateProjectState updateProjectSummary event -> 
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

  
  let addCommentWorkflow
    (logger : Logger)
    (logEvent:LogAddNewCommentEvent)
    : AddCommentWflow =

    fun projNum leaseProjectState addComment updateProjectState updateProjectSummary event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! dto, leaseId = leaseProjectState logger projNum () |> TaskResult.ofTask
        let! currentState = dto |> ProjectStateDto.toProjectState |> TaskResult.ofResult
        let! newState = addComment currentState eventBody |> TaskResult.ofResult
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

  
  let addAttchmentWorkflow
    (logger : Logger)
    (logEvent:LogAddAttachementEvent)
    : AddAttchmentWflow =

    fun projNum leaseProjectState addAttachement updateProjectState updateProjectSummary event -> 
      taskResult {

        logger Info <| sprintf "Recieved Event:\r\n%A" event

        let eventBody = logEvent logger event ()
        let! dto, leaseId = leaseProjectState logger projNum () |> TaskResult.ofTask
        let! currentState = dto |> ProjectStateDto.toProjectState |> TaskResult.ofResult
        let! newState = addAttachement currentState eventBody |> TaskResult.ofResult
        do! updateProjectState logger newState leaseId |> TaskResult.ofTask
        do! updateProjectSummary logger newState

        return newState
      }