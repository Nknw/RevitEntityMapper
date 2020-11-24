module Visitor
open TypeResolver
open System.Reflection

type 'k Result = 
    |Success of 'k
    |Failure of string

type InitResult<'a,'b> = 
    |Complited of 'a
    |NeedsCreate of 'b 

let continueSuccess cont = function
    | Success(s) -> cont s
    | Failure(s) -> Failure(s)

let getPropType (prop:PropertyInfo) = prop.PropertyType

type StepContext<'a,'b> = {
    info : PropertyInfo
    visitor : EntityDef -> 'a Result 
    eType : EntityType
    stepState : 'b
    }

let visitorBuilder init step finallize =
    let rec visitor entity =
        let rec iter  = 
            function
            |(state,[]) -> Success(state)
            |(state,h::tail) -> let next eType =
                                    step { info = h; visitor = visitor;
                                           eType = eType; stepState = state; 
                                           }
                                     |> continueSuccess (fun s-> iter (s,tail))
                                
                                match h |> getPropType |> Init |> getType with
                                | Error(s) -> Failure(s)
                                | Entity(_) | Map(_) | Array(_) | Simple(_) as t -> t |> next
                                | _ -> Failure("Unhandled")
                                       
        match entity |> init with 
        | NeedsCreate(s) -> iter (s,getProps entity) |> continueSuccess finallize
        | Complited(cs) -> cs
    visitor

let higthLevelVisitorBuilder visitor =
    let hlVisitor t =
        match t |> Init |> isEntity  with
        | Error(s) -> Failure(s)
        | Entity(e) -> e |> visitor 
        | _ -> Failure("Unhandled")
    hlVisitor
