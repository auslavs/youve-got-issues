module internal YGI.InternalTypes

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
  
  type LogNewIssueEvent       = LogEvent<NewIssueDto>
  type LogIssueUpdateEvent    = LogEvent<IssueUpdateDto>
  type LogAddNewCommentEvent  = LogEvent<NewCommentDto>
  type LogNewProjectEvent     = LogEvent<NewProjectDto>
  type LogAddAttachementEvent = LogEvent<AttachmentDetailsDto []>

  type UpdateIssue            = ApplyEvent<ProjectState,IssueUpdateDto>
  type AddNewComment          = ApplyEvent<ProjectState,NewCommentDto>
  type AddIssueToProject      = ApplyEvent<ProjectState,NewIssueDto>
  type AddAttachement         = ApplyEvent<ProjectState,AttachmentDetailsDto []>

  type LeaseProjectState      = LeaseCurrentState<ProjectStateDto>
  type StoreProjectState      = StoreState<ProjectState>
  type UpdateProjectState     = UpdateState<ProjectState>
  type CreateNewProject       = NewProjectDto -> Result<ProjectState, string>
  type CreateNewIssue         = Logger -> string -> LogNewIssueEvent -> NewProjectDto -> Result<ProjectState, string>
  type UpdateProjectSummary   = Logger -> ProjectState -> TaskResult<unit, string>
  