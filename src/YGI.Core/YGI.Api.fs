module YGI.Api

  open StorageHelpers
  open Microsoft.Extensions.Logging
  open FSharp.Control.Tasks.V2
  open YGI.Common
  open YGI.Dto
  open YGI.Implementation
  open YGI.Storage
  open YGI.Events

  let eventsTable = (getTableReference Constants.EventTable).Result

  let logNewProjectEvent : LogNewProjectEvent =
    fun logger evt () -> 
      StoreEvent eventsTable AddNewProjectEvent logger evt () |> ignore
      evt.State

  let createNewProject : CreateNewProject = 
    fun newProjectDto -> 
      NewProjectDto.toProjectState newProjectDto

  let storeProjectState : StoreProjectState =
    fun logger state -> 
      let blobPath = ProjectState.toProjectNumberStr state |> Constants.projectBlobPath
      let stateDto = state |> ProjectStateDto.fromProjectState
      StoreDto logger Constants.ContainerId "ProjectState" blobPath stateDto

  let updateProjectSummary : UpdateProjectSummary = 
    fun logger state -> 
      taskResult {
        let! currSummaryDto = getProjectSummary logger () |> TaskResult.ofTask
        let! currSummary    = SummaryDto.fromSummaryDto currSummaryDto |> TaskResult.ofResult
        let  projSummary    = ProjectState.toProjectSummary state
        let  newSummary     = Summary.addProject currSummary projSummary
        let  newSummaryDto  = SummaryDto.toSummaryDto newSummary

        return! StorageHelpers.StoreDto 
                  logger
                  Constants.ContainerId
                  "SummaryDto"
                  Constants.Summary newSummaryDto 
                |> TaskResult.ofTask
      }

  let newProjectApi =
    fun (logger:ILogger) evt ->

        let workflow = 
            Implementation.addNewProjectWorkflow
              logger
              logNewProjectEvent
              createNewProject
              storeProjectState
              updateProjectSummary

        workflow evt

  let logNewIssueEvent : LogNewIssueEvent =
    fun logger evt () -> 
      StoreEvent eventsTable AddNewIssueEvent logger evt () |> ignore
      evt.State

  let loadProjectState : LoadProjectState = 
    fun logger projNum () -> 
      task {

        let blobPath = Constants.projectBlobPath projNum
        let! dtoOption = StorageHelpers.retrieveDto<ProjectStateDto> logger Constants.ContainerId blobPath
    
        let projDto = 
          match dtoOption with
          | Some dto -> dto
          | None -> failwithf "Could not retreive the project state: %s" projNum

        return projDto
      }

  let addIssueToProject : AddIssueToProject = 
    fun (state:ProjectState) (evt:NewIssueDto) -> result {
      let! unvalidatedNewIssue = NewIssueDto.toUnvalidatedIssue evt
      return ProjectState.addIssue state unvalidatedNewIssue
      }
      

  let newIssueApi =
    fun (logger:ILogger) projNum evt ->

        let workflow = 
            Implementation.addNewProjectIssueWorkflow
              logger
              logNewIssueEvent
              loadProjectState
              addIssueToProject
              storeProjectState
              updateProjectSummary

        workflow projNum evt