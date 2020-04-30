namespace YGI

module Project =

  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open GiraffeViewEngine
  open GiraffeViewEngine.Accessibility
  open YGI.Dto
  open YGI.Events


  let projectView (state:ProjectStateDto) =

    let issues = state.Issues |> Array.toList
    let areas = state.AreaList |> Array.toList
    let equipTypes = state.EquipmentTypes |> Array.toList
    let issueTypes = state.IssueTypes |> Array.toList

    let headers =
      tr [] [ 
        th [] [str "Item No." ]
        th [] [str "Area" ]
        th [] [str "Equipment" ]
        th [] [str "Issue Type" ]
        th [] [str "Issue" ]
        th [] [str "Raised By" ]
        th [] [str "Raised" ]
        th [] [str "LastChanged" ]
        th [] [str "Status" ]
      ]
  
    let tableRow (i:IssueDto) = 
      tr [] [ 
        td [] [ str (i.ItemNo.ToString()) ]
        td [] [ str i.Area ]
        td [] [ str i.Equipment ]
        td [] [ str i.IssueType ]
        td [] [ str i.Issue ]
        td [] [ str i.RaisedBy ]
        td [] [ str (i.Raised.ToShortDateString()) ]
        td [] [ str (i.LastChanged.ToShortDateString()) ]
        td [] [ str (i.Status.ToString()) ]
      ]

    let modal =

      let area      = Components.formFreeSelect "area" "Area" "Area" areas
      let equipment = Components.formFreeSelect "equipment" "Equipment" "Equipment" equipTypes
      let issueType = Components.formSelect "issue-type" "IssueType" "Issue Type" issueTypes
      let issue     = Components.formTextbox "issue" "Issue" "Issue"
      let raisedBy  = Components.formTextbox "raised-by" "RaisedBy" "Raised By"

      div [ _class "modal fade"; _id "new-issue-modal"; _tabindex "-1"; _ariaRoleDescription "dialog"; _ariaLabelledBy "New Issue"; _ariaHidden "true" ] [
        div [ _class "modal-dialog modal-dialog-centered modal-lg" ] [
          div [ _class "modal-content"] [
            form [ _action ""; _method "post"; _roleForm ] [
              // Header
              div [ _class "modal-header"] [
                h5 [ _class "modal-title"; _id "new-issue-modal-header" ] [ str "Add New Issue"]
                button [ _type "button"; _class "close"; _data "dismiss" "modal"; _ariaLabel "Close" ] [
                  span [ _ariaHidden "true" ] [ rawText "&times;" ] ]
              ]
              // Body
              div [ _class "modal-body" ] [
                area; equipment; issueType; issue; raisedBy
                ]

              // Footer
              div [ _class "form-group"] [
                div [ _class "modal-footer" ] [
                  button [ _type "button";  _class "btn btn-secondary"; _data "dismiss" "modal" ] [ str "Close" ]
                  button [ _type "submit";  _class "btn btn-primary"; ] [ str "Save changes" ] ] ]
            ]
          ]
        ]
      ]

    let tableContent = (headers::(issues |> List.map tableRow)) 
      
    [ 
      p [] [ str "" ]
      div [ _class "card" ] [
        button [ _type "button"; _class "btn btn-primary"; _data "toggle" "modal"; _data "target" "#new-issue-modal" ] [ str "+ Add New" ]
      ]
      p [] [ str "" ]
      table [ _class "table table-hover table-sm" ] tableContent 
      modal
    ]
    |> Components.layout Components.Login


  let getHandler proj =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
          let logger = Logging.log <| ctx.GetLogger()
          let! summary = Storage.getProject logger proj ()

          match summary with 
          | Some s -> 
            let view = htmlView (projectView s)
            return! view next ctx
          | None -> 
            let response = RequestErrors.NOT_FOUND "Page not found"
            return! response next ctx            
        }

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

  //let createNewIssue projNum =
  //  fun (next : HttpFunc) (ctx : HttpContext) ->
  //    task {
  //      let logger = Logging.log <| ctx.GetLogger()
  //      let! dto = ctx.BindFormAsync<NewIssueDto>()

  //      let taskResult = taskResult {
  //        let event = AddNewIssueEvent.create dto ""
  //        return! Api.newIssueApi logger projNum event
  //      }

  //      let response = taskResult |> Task.map (fun r -> 
  //        match r with
  //        | Ok _ -> redirectTo false "/" next ctx
  //        | Error err -> failwithf "createNewIssue failed: %s" err
  //      )

  //      return! redirectTo false "/" next ctx
  //    }

  let createNewIssue projNum : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<NewIssueDto>()

        let! taskResult = taskResult {
          let event = AddNewIssueEvent.create dto ""
          return! Api.CreateNewIssue logger projNum event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }
  