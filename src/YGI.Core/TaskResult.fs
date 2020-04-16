namespace YGI

open System.Threading.Tasks
open FSharp.Control.Tasks.V2

module Task =

    /// Lift a function to Task
    let map (f:'a -> 'b) (xA:Task<'a>) = 
        task { 
        let! x = xA
        return f x 
        }

    /// Lift a value to Task
    let retn x = 
        task { return x }


type TaskResult<'Success,'Failure> = 
  Task<Result<'Success,'Failure>>

module TaskResult = 

  /// Lift a function to TaskResult
  let map f (x:TaskResult<_,_>) : TaskResult<_,_> =
      Task.map (Result.map f) x

  /// Lift a value to TaskResult
  let retn x : TaskResult<_,_> = 
      x |> Result.Ok |> Task.retn

  /// Apply a monadic function to an TaskResult value  
  let bind (f: 'a -> TaskResult<'b,'c>) (xTaskResult : TaskResult<_, _>) :TaskResult<_,_> = task {
      let! xResult = xTaskResult
      match xResult with
      | Ok x -> return! f x
      | Error err -> return (Error err)
      }

  /// Lift a Result into an TaskResult
  let ofResult (x:Result<'a,'b>) : TaskResult<'a,'b> = 
    x |> Task.retn

  /// Lift a Task into an TaskResult
  let ofTask (x:Task<'a>) : TaskResult<'a,'b> = 
    x |> Task.map Result.Ok

[<AutoOpen>]
module TaskResultComputationExpression = 

    type TaskResultBuilder() = 
        member __.Return(x) = TaskResult.retn x
        member __.Bind(x, f) = TaskResult.bind f x

        member __.ReturnFrom(x) = x
        member this.Zero() = this.Return ()

        member __.Delay(f) = f
        member __.Run(f) = f()

        member this.While(guard, body) =
            if not (guard()) 
            then this.Zero() 
            else this.Bind( body(), fun () -> 
                this.While(guard, body))  

        member this.TryWith(body, handler) =
            try this.ReturnFrom(body())
            with e -> handler e

        member this.TryFinally(body, compensation) =
            try this.ReturnFrom(body())
            finally compensation() 

        member this.Using(disposable:#System.IDisposable, body) =
            let body' = fun () -> body disposable
            this.TryFinally(body', fun () -> 
                match disposable with 
                    | null -> () 
                    | disp -> disp.Dispose())

        member this.For(sequence:seq<_>, body) =
            this.Using(sequence.GetEnumerator(),fun enum -> 
                this.While(enum.MoveNext, 
                    this.Delay(fun () -> body enum.Current)))

        member this.Combine (a,b) = 
            this.Bind(a, fun () -> b())

    let taskResult = TaskResultBuilder()