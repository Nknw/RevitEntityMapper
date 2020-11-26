module Visitor
open TypeResolver
open System.Reflection
open Autodesk.Revit.Mapper

type InitResult<'a,'b> = 
    |Complited of 'a
    |NeedsCreate of 'b 

let getPropType (prop:PropertyInfo) = prop.PropertyType

type StepContext<'a,'b> = {
    info : PropertyInfo
    visitor : EntityDef -> 'a 
    eType : EntityType
    stepState : 'b
    }

let visitorBuilder init step finallize =
    let rec visitor entity =
        let rec iter  = 
            function
            |(state,[]) -> state
            |(state,h::tail) -> let next eType =
                                    let newState = { info = h; visitor = visitor;
                                                     eType = eType; stepState = state; 
                                                     } |> step 
                                    (newState,tail) |> iter
                                
                                match h |> getPropType |> Init |> getType with
                                | Entity(_) | Map(_) | Array(_) | Simple(_) as t -> t |> next
                                | Init(_) -> raise (new MapperException("no one handle input"))
                                       
        match entity |> init with 
        | NeedsCreate(s) -> iter (s,getProps entity) |> finallize
        | Complited(cs) -> cs
    visitor

let getEntityDefenition t =
    match t |> Init |> isEntity  with
    | Entity(e) -> e  
    | _ ->  raise (new MapperException(log "{0} has no SchemaAttribute" [t]))
