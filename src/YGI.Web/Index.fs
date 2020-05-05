namespace YGI

module Index =

  open Giraffe
  open Microsoft.AspNetCore.Http
  open FSharp.Control.Tasks.V2
  open GiraffeViewEngine
  open GiraffeViewEngine.Accessibility
  open YGI.Dto
  open YGI.Events

  let indexView (summary:SummaryDto) =

      let projList = summary.ProjList |> Array.toList
  
      let headers =
        tr [] [ 
          th [] [str "Project Number" ]
          th [] [str "Project Name" ]
          th [] [str "Open Issues" ]
        ]
  
      let tableRow (i:ProjectSummaryDto) = 
        tr [] [ 
          td [] [ a [ _href i.ProjectNumber] [ str i.ProjectNumber ] ]
          td [] [ str i.ProjectName ]
          td [] [ str i.OpenIssues ]
        ]

      let modal =
        div [ _class "modal fade"; _id "new-project-modal"; _tabindex "-1"; _ariaRoleDescription "dialog"; _ariaLabelledBy "exampleModalLabel"; _ariaHidden "true" ] [
          div [ _class "modal-dialog modal-dialog-centered modal-lg" ] [
            div [ _class "modal-content"] [
              form [ _action ""; _method "post"; _roleForm ] [
                // Header
                div [ _class "modal-header"] [
                  h5 [ _class "modal-title"; _id "exampleModalLabel" ] [ str "Add New Project"]
                  button [ _type "button"; _class "close"; _data "dismiss" "modal"; _ariaLabel "Close" ] [
                    span [ _ariaHidden "true" ] [ rawText "&times;" ] ]
                ]
                // Body
                div [ _class "modal-body" ] [
                  div [ _class "form-group"] [ 
                    label [ _for "name" ] [ str "Project Number" ]
                    input [ _id "name"; _name "ProjectNumber"; _class "form-control"; _type "text"; _required ] ]
                  div [ _class "form-group"] [ 
                    label [ _for "description" ] [ str "Project Name" ]
                    input [ _id "description"; _name "ProjectName"; _class "form-control"; _type "text" ] ]

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
        
      let tableContent = (headers::(projList |> List.map tableRow))
      
      [ 
        p [] [ str ""]
        div [ _class "card" ] [
          button [ _type "button"; _class "btn btn-primary"; _data "toggle" "modal"; _data "target" "#new-project-modal" ] [ str "+ Add New" ]
        ]
        p [] [ str ""]
        table [ _class "table" ] tableContent 
        modal
      ]
      |> Components.layout Components.Login


  let getHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let! summary = Storage.getProjectSummary ()
        let view = htmlView (indexView summary)
        return! view next ctx
      }

  let getApiHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let! summary = Storage.getProjectSummary ()
        return! (Successful.OK summary) next ctx 
      }

  //let createNewProject =
  //  fun (next : HttpFunc) (ctx : HttpContext) ->
  //    task {
  //      let logger = Logging.log <| ctx.GetLogger()
  //      let! dto = ctx.BindFormAsync<NewProjectDto>()

  //      let taskResult = taskResult {
  //        let event = AddNewProjectEvent.create dto ""
  //        return! Api.CreateNewProject logger event
  //      }

  //      let response = taskResult |> Task.map (fun r -> 
  //        match r with
  //        | Ok _ -> redirectTo false "/" next ctx
  //        | Error err -> failwithf "createNewProject failed: %s" err
  //      )

  //      return! redirectTo false "/" next ctx
  //    }

  let createNewProject =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
        let logger = Logging.log <| ctx.GetLogger()
        let! dto = ctx.BindJsonAsync<NewProjectDto>()

        let! taskResult = taskResult {
          let event = YgiEvent.create "" dto.ProjectNumber dto
          return! Api.CreateNewProject logger event
        }

        let response =
          match taskResult with
          | Ok _ -> (Successful.OK "") next ctx
          | Error err -> (RequestErrors.BAD_REQUEST err) next ctx

        return! response
      }
  