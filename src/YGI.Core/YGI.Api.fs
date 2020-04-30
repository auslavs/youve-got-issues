﻿module YGI.Api

  open StorageHelpers
  open FSharp.Control.Tasks.V2
  open YGI.Logging
  open YGI.Common
  open YGI.Dto
  open YGI.Implementation
  open YGI.Storage
  open YGI.Events

  let private eventsTable = (getTableReference Constants.EventTable).Result

  let private logNewProjectEvent : LogNewProjectEvent =
    fun logger evt () -> 
      StoreEvent eventsTable AddNewProjectEvent logger evt () |> ignore
      evt.State

  let private createNewProject : CreateNewProject = 
    fun newProjectDto -> 
      NewProjectDto.toProjectState newProjectDto

  let private storeNewProjectState : StoreProjectState =
     fun logger projState -> 
      task{
        let stateDto = projState |> ProjectStateDto.fromProjectState
        return! storeNewProject logger stateDto ()
      }

  let private updateProjectState : UpdateProjectState =
    fun logger projState leaseId -> 
      let stateDto = projState |> ProjectStateDto.fromProjectState
      Storage.updateProject logger stateDto leaseId ()

  let private updateProjectSummary : UpdateProjectSummary = 
    fun logger projState -> 
      taskResult {
        let! dto, leaseId  = leaseProjectSummary logger () |> TaskResult.ofTask
        let! currSummary   = SummaryDto.fromSummaryDto dto |> TaskResult.ofResult
        let  projSummary   = ProjectState.toProjectSummary projState
        let  newSummary    = Summary.addOrReplaceProject currSummary projSummary
        let  newSummaryDto = SummaryDto.toSummaryDto newSummary

        return! Storage.updateProjectSummary logger newSummaryDto leaseId ()
                |> TaskResult.ofTask
      }

  let private addIssueToProject : AddIssueToProject = 
    fun (state:ProjectState) (evt:NewIssueDto) -> result {
      let! unvalidatedNewIssue = NewIssueDto.toUnvalidatedIssue evt
      return ProjectState.addIssue state unvalidatedNewIssue
      }

  let private logNewIssueEvent : LogNewIssueEvent =
    fun logger evt () -> 
      StoreEvent eventsTable AddNewIssueEvent logger evt () |> ignore
      evt.State

  let CreateNewProject =
    fun (logger:Logger) evt ->

        let workflow = 
            Implementation.addNewProjectWorkflow
              logger
              logNewProjectEvent
              createNewProject
              storeNewProjectState 
              updateProjectSummary

        workflow evt

  let CreateNewIssue =
    fun (logger:Logger) projNum evt ->

        let workflow = 
            Implementation.addNewProjectIssueWorkflow
              logger
              logNewIssueEvent
              leaseProject
              addIssueToProject
              updateProjectState
              updateProjectSummary

        workflow projNum evt


  let private getProjectState logger projNum : GetProjectState =
      taskResult {

        let! projOpt = getProject logger projNum () |> TaskResult.ofTask

        let result = 
          match projOpt with
          | None -> Result.Error ""  
          | Some proj -> Result.Ok proj

        return! result |> TaskResult.ofResult
      }

  let private createIssueDetail issueNum : CreateIssueDetailViewDto =
    fun state ->
      IssueDetailViewDto.fromProjectStateDto issueNum state


  let GetIssueModel =
    fun (logger:Logger) projNum (issueNum:int) ->

      let getProjectState = getProjectState logger projNum
      let createIssueDetail = createIssueDetail issueNum
      
      let workflow = 
        Implementation.getIssueDetailWorkFlow
          logger
          getProjectState
          createIssueDetail

      workflow projNum issueNum